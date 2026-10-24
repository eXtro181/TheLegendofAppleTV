# Unity-Version (Unity 6, URP, WebGL)

Dieser Ordner ist ein vollständiges Unity-Projekt. Es wird **automatisch von GitHub gebaut** (GitHub Actions + GameCI)
und landet danach unter `https://extro181.github.io/TheLegendofAppleTV/unity3d/`.

## Einmalig nötig: Unity-Lizenz als Secret

Unity verlangt auch für Builds auf GitHub eine (kostenlose) Personal-Lizenz. Die Lizenzdatei lässt sich nur mit
Unity Hub erzeugen – einmal, auf irgendeinem Mac/Windows/Linux-Rechner (auch geliehen):

1. Unity Hub installieren, mit deinem Unity-Konto anmelden.
2. Einstellungen → Lizenzen → Hinzufügen → „Kostenlose Personal-Lizenz“.
3. Datei `Unity_lic.ulf` öffnen:
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - Mac: `/Library/Application Support/Unity/Unity_lic.ulf`
4. Im Repo auf GitHub: Settings → Secrets and variables → Actions → „New repository secret“:
   - `UNITY_LICENSE` = gesamter Inhalt der .ulf-Datei
   - `UNITY_EMAIL` = E-Mail des Unity-Kontos
   - `UNITY_PASSWORD` = Passwort des Unity-Kontos
5. Actions → „Unity WebGL“ → „Run workflow“.

## Aufbau

- `Assets/Editor/Builder.cs` richtet beim Build alles per Skript ein: URP mit SSAO, HDR, weiche Schatten (4096),
  Materialien, Himmel, Szene – und baut WebGL (ASTC-Texturen fürs iPhone).
- `Assets/Scripts/LU/` Spielcode: Stadtgenerator (wie Browser-Version), Figuren (glTFast), Steuerung, Kamera.
- `Assets/Shaders/LitVC.shader` PBR mit Vertexfarbe (Fassadentönung, Schmutzkante) und Figuren-Regionen.
- Texturen: Quaternius-Kit in voller 2048er-Auflösung (Browser-Version: 1024).
