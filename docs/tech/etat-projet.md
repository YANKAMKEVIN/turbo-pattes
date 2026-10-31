# Turbo Pattes — état du projet

Jeu de karting arcade original (nom de travail « Turbo Pattes ») : pilotes animaux cartoon, drift qui charge un turbo,
objets ramassés sur la piste, 8 pilotes, 3 tours. Codes du genre, mais personnages, circuits, objets et interface
propres au jeu (pas de reprise de Mario Kart ni d'aucun autre jeu existant).

Dernière mise à jour : 2026-10-31.

## Décisions prises (avec Kevin)

| Sujet | Décision |
|---|---|
| Moteur | Unity 6000.6.3f1 + URP 17 + Input System (configuration reprise de `~/dev/check`) |
| Plateformes | Mobile Android **et** PC/manette dès le départ (iOS plus tard) |
| Univers | Animaux cartoon |
| Première étape | Prototype jouable : conduite, drift/turbo, un circuit, IA |
| Identifiant | `com.kevinyankam.turbopattes`, écran en paysage |

## Contenu du prototype

- **Circuit** « Prairie des Pattes » (~1,1 km, rayon mini ~15 m) : spline fermée, bitume, vibreurs, murets, portique de départ, tribune, arbres, collines, nuages. Tout est généré par code.
- **Pilotes** (8, modèles en primitives) : Roxy (renarde, joueuse), Bambou (panda), Flip (grenouille), Lili (lapine), Bruno (ours), Mimi (chatte), Pingo (pingouin), Rosa (cochonne).
- **Conduite** arcade sans moteur physique (`KartMotor`) : saut + drift à 3 niveaux (étincelles jaunes, vertes, roses), turbo au lâcher, hors-piste lent, rebond sur les murets, chocs entre karts, super départ (accélérer dans la dernière seconde du compte à rebours), tapis turbo.
- **Objets** (bulles surprise à pattes dorées) : carotte turbo, noix de coco (roule tout droit, rebondit), pot de miel (flaque derrière soi), bulle bouclier. Tirage selon la place (`ItemRoll`).
- **IA** : trajectoire avec corde dans les virages, freinage selon la courbure, drift dans les grandes courbes, évitement du miel, usage des objets, élastique léger.
- **Commandes** : clavier (flèches/WASD/ZQSD, Espace = drift, E = objet, R = rejouer), manette (stick, RT/A, LT/B, RB = drift, X/Y = objet, Start), tactile (glisser à gauche pour tourner, boutons DRIFT / OBJET / FREIN, accélération auto).
- **Interface** IMGUI (provisoire) : tour, chrono, place, objet avec roulette, classement, vitesse, compte à rebours, « Mauvais sens ! », résultats + Rejouer.
- **Sons** synthétisés par code (moteur, bips, turbo, chocs, objets).

## Architecture

```
Assets/TurboPattes/
  Core/    logique pure testable (sans UnityEngine) : LapTracker, DriftCharge, ItemRoll, RaceRanking
  Game/
    App/      KartApp : point d'entrée (seul objet de la scène), construit le monde ; mode autotest
    Track/    Circuit (données), TrackPath (spline + requêtes), TrackBuilder (maillages et décor)
    Kart/     KartMotor (conduite), KartFactory + Animals (modèles), AiDriver, PlayerInput, KartInput
    Items/    ItemSystem (bulles, coco, miel, effets)
    Race/     RaceManager (déroulé de course), ChaseCamera
    UI/       Hud
    Visuals/  Mats (matériaux depuis gabarits Resources), ProceduralTextures, MeshKit
    Audio/    SoundBank
  Editor/  BuildScript (configuration, gabarits de matériaux, builds)
  Resources/  gabarits de matériaux générés par BuildScript.Setup (embarquent les shaders URP)
tests/TurboPattes.Tests/  tests NUnit hors Unity du dossier Core
```

## Commandes

```bash
cd ~/dev/turbo-pattes/tests/TurboPattes.Tests && DOTNET_CLI_TELEMETRY_OPTOUT=1 /Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet test
/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath ~/dev/turbo-pattes -executeMethod TurboPattes.EditorTools.BuildScript.BuildMac -logFile -
/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath ~/dev/turbo-pattes -executeMethod TurboPattes.EditorTools.BuildScript.BuildAndroid -logFile -
# Course automatique (le joueur est piloté par l'IA), captures + journal dans le dossier :
~/dev/turbo-pattes/Builds/macOS/TurboPattes.app/Contents/MacOS/"Turbo Pattes" -tp-autotest /tmp/tp-autotest -tp-laps 1
```

## Vérifications faites (2026-10-31)

- 16 tests NUnit verts sur le Core.
- Build Mac OK : course autotest de 1 tour sans erreur ni exception, ~60 i/s, joueur (piloté par l'IA) 4e en 42,8 s, peloton serré (~2 s). Captures vérifiées : rendu, HUD, résultats.
- Correction faite après la première course : l'IA coupait la boucle serrée par l'herbe (visée trop lointaine).
- Build Android OK (APK 32 Mo, `Builds/Android/TurboPattes.apk`) mais **pas encore installé** : le S24 s'est déconnecté. Installation : `~/Library/Android/sdk/platform-tools/adb install -r Builds/Android/TurboPattes.apk`.
- Non testé : commandes tactiles et manette sur un vrai appareil, ressenti du drift par un humain.

## Pièges connus

- Matériaux : toujours passer par `Mats` (clones des gabarits de `Resources`), sinon les shaders URP peuvent manquer dans le build.
- Brouillard : la scène enregistrée a le brouillard activé pour que ses variantes de shader ne soient pas retirées du build.
- Hérités de Check/KevChess : « Strip Unused Post Processing Variants » désactivé (sinon écran noir Android) ; Mac = Mono seulement ; un seul éditeur Unity par dossier.

## Prochaines étapes envisagées

1. Faire tester le ressenti de conduite par Kevin (S24 + manette) et régler vitesses, drift, IA.
2. Interface en uGUI avec icônes d'objets dessinées, menu de départ et choix du pilote.
3. Deuxième circuit, relief (ponts, sauts), plus d'objets propres au jeu.
4. Modèles 3D plus fins pour les pilotes (ou génération), musique.
