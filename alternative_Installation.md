# City2RVT - Installation ohne Administratorrechte

In dieser Anleitung wird beschrieben, wie Sie das Plugin "City2RVT" ohne Administratorrechte installieren.

Es werden zwei Varianten beschrieben. Die Anleitungen haben den gleichen Namen wie die Ordner, die Sie dafür herunterladen müssen.

Im Abschnitt "City2RVT_20XX_alternativ" wird beschrieben, wie Sie die Dateien des fertigen Plugins in Revit einbinden. Dafür laden Sie die fertigen Dateien des Plugins herunter. Weiterhin editieren und verschieben Sie eine Datei. 

Im Abschnitt "Source Code" wird beschrieben, wie Sie das Projekt des Plugins erstellen und anschließend einbinden. Hierfür laden Sie den Programmcode des Plugins herunter. 

## City2RVT_20XX_alternativ

### Voraussetzung

- Texteditor (z.B.: Notepad++)

### Durchführung

Im Folgenden werden die letzten beiden Stellen der Jahreszahl mit XX beschrieben. Ersetzen Sie diese mit den letzten beiden Stellen der Jahreszahl der Revit Version, mit der Sie arbeiten.

Laden Sie den Ordner "*City2RVT_20XX_alternativ*" herunter und entpacken ihn. Anschließend speichern Sie ihn an einem beliebigen Ort Ihrer Wahl. Navigieren Sie in diesem Ordner zu der Datei *City2RVT.dll* (*City2RVT_20XX_alternativ\City2RVT\City2RVT.dll*).  Der Pfad der Datei soll kopiert werden. Hierfür öffnen Sie das Kontextmenü durch drücken von *Shift + rechten Mausklick (RMK)*. Anschließend klicken Sie *Als Pfad kopieren*.

Öffnen Sie nun die Datei *City2BIM.addin* in einem Texteditor. In Zeile 10 steht im *Assembly* Tag aktuell "City2RVT\City2RVT.dll".  Diesen Ausdruck löschen Sie jetzt. Fügen Sie an dieser Stelle den kopierten Pfad der Datei *City2RVT.dll* ein. Die Anführungszeichen(") an Anfang und Ende des Pfades müssen Sie löschen. Speichern Sie die Datei und schließen den Editor.

Nun ist es notwendig, die Datei *City2BIM.addin* in Revit einzubinden. Damit Revit diese Datei automatisch verarbeitet, muss sie an einem von zwei möglichen Orten gespeichert werden. Die zwei Möglichkeiten sind ein benutzerspezifischer und ein nicht benutzerspezifischer Ort.

#### Benutzerspezifisch

Kopieren Sie die Datei *City2BIM.addin*. Navigieren Sie zu dem Ordner "*C:\Users\\\AppData\Roaming\Autodesk\Revit\Addins\20XX*".  Fügen Sie hier die kopierte Datei *City2BIM.addin* ein. Um den Ordner "*AppData*" zu öffnen, geben Sie in die Suchleiste "%AppData%" ein.

#### Nicht benutzerspezifisch

Kopieren Sie die Datei *City2BIM.addin*. Navigieren Sie zu dem Ordner "*C:\ProgramData\Autodesk\Revit\Addins\20XX*".  Fügen Sie hier die kopierte Datei *City2BIM.addin* ein. Der Ordner "*ProgramData*" ist verborgen. Um ihn unter Nutzung von Windows anzuzeigen, öffnen Sie das Fenster *Ordneroptionen* (*Ansicht* -> *Optionen*). Nun wählen Sie den Reiter *Ansicht* und unter "*Versteckte Dateien und Ordner*" den Punkt "*Ausgeblendete Dateien, Ordner und Laufwerke anzeigen*" .

Nun ist das Plugin in Revit eingebunden. Beim Starten von Autodesk Revit sollte das Plugin vorhanden sein. Sollte beim Öffnen des Programms gefragt werden, ob das Plugin geladen werden soll, bestätigen Sie dies bitte mit *Immer laden*.

## Source Code

### Voraussetzung

- Visual Studio
  - Öffnen der Visual Studio Solution
  - Beschreibung von Befehlen/Funktionen auf Basis des deutschen Sprachpaketes
- Texteditor (z.B.: Notepad++)

### Durchführung

Laden Sie den Ordner "*Source Code*" herunter und entpacken ihn. Anschließend speichern Sie ihn an einem beliebigen Ort Ihrer Wahl.

#### Visual Studio

Anschließend navigieren Sie in den Ordner und öffnen die darin befindliche Datei *City2BIM.sln* mit Visual Studio. Ihr Projektmappen-Explorer (standardmäßig auf der echten Seite des Bildschirms) sollte so aussehen:

![Projektmappe](pic/city2bim_projektmappe.png)

Zunächst müssen Sie die Projektmappenkonfigurationen und Projektmappenplattformen ändern. Diese finden Sie in der Standard Symbolleiste:

![Symbolleiste](pic/standard_symbolleiste.png)

Öffnen Sie das Dropdown Menü indem Sie den nach unten zeigenden Pfeil hinter *Debug* klicken. Hier wählen Sie den *Konfigurations-Manager...* . Im *Konfigurations-Manager* stellen Sie die *Konfiguration der aktuellen Projektmappe:* auf *Release* und die *Aktive Projektmappenplattform:* auf *x64*. Anschließend schließen Sie das Fenster.

Im *Projektmappen-Explorer* öffnen Sie das Kontextmenü durch einen rechten Mausklick (RMK) auf das Projekt *City2BIM*. Hier wählen Sie *Neu erstellen*. Nun wird das Projekt erstellt. Diesen Vorgang wiederholen Sie für das Projekt *City2IFC*. 

##### Hinweis: Sollten in den Projekten im Abschnitt *Verweise* noch gelbe Dreiecke auftauchen, liegt das wahrscheinlich an der Aktualisierung. Klicken Sie einmal einen dieser Verweise an, die gelben Dreiecke sollten nun verschwinden.

Navigieren Sie im Projekt *City2RVT* zum Abschnitt *Verweise*. Hier ist die Revit API mit den Verweisen *RevitAPI* und *RevitAPIUI* eingebunden. Diese müssen der Revit-Version entsprechen, welche Sie verwenden. Zurzeit ist die Revit API der Revit-Version 2019 eingebunden. Wollen Sie diese verwenden, können Sie den nächsten Absatz überspringen.

Um die Revit API der Version 2020 einzubinden, müssen zunächst die Verweise *RevitAPI* und *RevitAPIUI* entfernt werden. Hierfür RMK auf diese Verweise und *Aus Projektmappe entfernen* wählen. Anschließend RMK auf *Verweise* und *Verweis hinzufügen...* wählen. Im *Verweis-Manager - City2RVT* den Button *Durchsuchen..* wählen. Nun navigieren Sie zu den Dateien *RevitAPI.dll* und *RevitAPIUI.dll* und fügen diese hinzu. Die Dateien liegen standardmäßig unter dem Pfad: "C:\Program Files\Autodesk\Revit 2020\ *.dll". Das Ergebnis sollte folgendermaßen aussehen:

![Verweis-Manager](pic/verweis_manager.png)

Schließen Sie den *Verweis-Manager* durch Drücken des *OK*-Buttons.

Das Projekt *City2RVT* muss nun erstellt werden. Dafür RMK auf das Projekt und *Neu erstellen* wählen. 

Nun sind alle Vorkehrungen in Visual Studio getroffen. Nach dem Speichern kann Visual Studio geschlossen werden.

#### Explorer

Navigieren Sie im Explorer zum Plugin-Ordner und darin zur *City2RVT.dll*-Datei (Pfad: "*...\City2RVT\bin\x64\Release\City2RVT.dll*").  Dieser Pfad der Datei soll nun kopiert werden. Hierfür öffnen Sie das Kontextmenü durch drücken von *Shift + RMK*. Anschließend klicken Sie *Als Pfad kopieren*.

Öffnen Sie nun die Datei *City2BIM.addin* (im obersten Unterverzeichnis des Plugin Ordners) in einem Texteditor. In Zeile 10 steht im *Assembly* Tag aktuell "City2RVT\City2RVT.dll".  Diesen Ausdruck löschen Sie jetzt. Danach fügen Sie an dieser Stelle den kopierten Pfad der Datei *City2RVT.dll* ein. Löschen Sie die Anführungszeichen (")  am Anfang und am Ende des Pfades. Speichern Sie die Datei und schließen den Editor.

Jetzt ist es notwendig die Datei *City2BIM.addin* in Revit einzubinden. Damit Revit diese Datei automatisch verarbeitet, muss sie an einem von zwei möglichen Orten gespeichert werden. Die zwei Möglichkeiten sind ein benutzerspezifischer und ein nicht benutzerspezifischer Ort.

##### Benutzerspezifisch

Kopieren Sie die Datei *City2BIM.addin*. Navigieren Sie zu dem Ordner "*C:\Users\\\AppData\Roaming\Autodesk\Revit\Addins\20XX*". Fügen Sie hier die kopierte Datei *City2BIM.addin* ein. Um den Ordner "*AppData*" zu öffnen, geben Sie in die Suchleiste "%AppData%" ein.

##### Nicht benutzerspezifisch

Kopieren Sie die Datei *City2BIM.addin*. Navigieren Sie zu dem Ordner "*C:\ProgramData\Autodesk\Revit\Addins\20XX*". Fügen Sie hier die kopierte Datei *City2BIM.addin* ein. Der Ordner "*ProgramData*" ist verborgen. Um ihn unter Windows sichtbar zu machen, öffnen Sie das *Ordneroptionen*-Fenster (*Ansicht* -> *Optionen*).  Wählen Sie den Reiter *Ansicht* und wählen unter "*Versteckte Dateien und Ordner*" den Punkt "*Ausgeblendete Dateien, Ordner und Laufwerke anzeigen*" .

Nun ist das Plugin in Revit eingebunden. Beim Starten von Autodesk Revit sollte das Plugin vorhanden sein. Sollte beim Öffnen des Programms gefragt werden, ob das Plugin geladen werden soll, bestätigen Sie dies bitte mit *Immer laden*.
