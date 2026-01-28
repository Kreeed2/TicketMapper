# Betriebsbeschreibung: TicketMapper (SyncApp)

## 1. Einführung

Der **TicketMapper** ist ein .NET-basierter Hintergrunddienst (Worker Service), der für die automatische Synchronisation von Tickets und Arbeitselementen zwischen verschiedenen Systemen entwickelt wurde. In der aktuellen Implementierung unterstützt das Tool primär die Synchronisation von **TOPdesk**-Incidents zu **Azure DevOps**-Arbeitselementen (z. B. User Stories oder Bugs).

## 2. Systemvoraussetzungen

* **Laufzeitumgebung:** .NET 8.0 SDK oder Runtime (basierend auf der modernen C#-Syntax).
* **Netzwerkzugriff:** Die Anwendung benötigt ausgehende HTTPS-Verbindungen zu den APIs von TOPdesk und Azure DevOps.
* **Berechtigungen:** * **TOPdesk:** API-Benutzer mit Leserechten für Incidents.
* **Azure DevOps:** Personal Access Token (PAT) mit Schreibrechten für Work Items.

## 3. Konfiguration

Die Steuerung der Anwendung erfolgt über zwei zentrale JSON-Dateien im Anwendungsverzeichnis.

### 3.1 appsettings.json

Diese Datei steuert das allgemeine Anwendungsverhalten, insbesondere das Logging-Niveau.

* **LogLevel:** Hier kann definiert werden, wie detailliert die Anwendung protokolliert (z. B. `Information`, `Debug` oder `Error`).

### 3.2 config.json

Dies ist das "Herzstück" der Konfiguration, unterteilt in drei Bereiche:

1. **Settings:**

* `poll_interval_seconds`: Legt fest, wie viele Sekunden die Anwendung zwischen zwei Synchronisationszyklen pausiert (Standard: 10-30 Sek.).

1. **Systems:**

* Definition der Quell- und Zielsysteme.
* Erfordert `url`, `type` (topdesk, azuredevops), `auth_type` sowie Anmeldedaten (`token` oder `username`/`password`).

1. **Mappings:**

* Definiert, welche Felder von einem System in das andere übertragen werden.
* `external_id_field`: Ein Feld im Zielsystem, das die ID des Quellsystems speichert, um Duplikate zu vermeiden.
* `transform`: Bestimmt, ob Daten eins-zu-eins (`none`), über eine Nachschlagetabelle (`lookup`) oder als Festwert (`static`) übernommen werden.

## 4. Betrieb und Ausführung

### 4.1 Starten der Anwendung

Die Anwendung wird als Konsolenanwendung gestartet. Sie unterstützt Kommandozeilenparameter für spezielle Modi:

```bash
dotnet SyncApp.dll [--dry-run]

```

* **--dry-run:** In diesem Modus simuliert die Anwendung die Synchronisation. Es werden Änderungen erkannt und geloggt, aber keine Daten in den Zielsystemen erstellt oder verändert.

### 4.2 Der Synchronisationsprozess

Nach dem Start führt der `Worker` folgende Schritte aus:

1. **Validierung:** Beim Start prüft die Anwendung, ob alle konfigurierten Ziel-Felder in den Zielsystemen existieren.
2. **Abfrage:** Das Quellsystem wird nach Änderungen durchsucht.
3. **Abgleich:** Für jedes Element wird geprüft, ob es im Zielsystem bereits vorhanden ist (via `external_id_field`).
4. **Aktion:** * Wenn neu: Das Element wird im Zielsystem angelegt.

* Wenn vorhanden: Die Felder werden verglichen. Bei Unterschieden wird das Zielsystem aktualisiert (sofern `update: true` konfiguriert ist).

## 5. Wartung und Fehlerbehebung

### 5.1 Logging

Die Anwendung gibt alle Aktivitäten auf der Konsole aus.

* **Information:** Zeigt den normalen Verlauf des Synchronisationszyklus.
* **Error:** Tritt auf, wenn API-Aufrufe fehlschlagen oder die Konfiguration fehlerhaft ist.
* **Critical:** Führt zum Stopp der Anwendung (z. B. bei ungültiger Konfiguration während der Start-Validierung).

### 5.2 Test-Modus (Mocking)

Um die Logik zu testen, ohne echte APIs anzusprechen, kann in der `config.json` die URL eines Systems das Wort **"mock"** enthalten. Die `ClientFactory` erstellt dann automatisch einen `MockClient`, der Testdaten liefert, anstatt echte HTTP-Anfragen zu senden.

### 5.3 Typische Fehlerquellen

* **Abgelaufene Tokens:** Wenn die Verbindung zu Azure DevOps fehlschlägt, prüfen Sie die Gültigkeit des PAT.
* **Fehlende Felder:** Wenn die Validierung beim Start fehlschlägt, existiert ein in der `config.json` definiertes `target`-Feld nicht im Zielsystem.