# Turbo Pattes — état du projet

Jeu de karting arcade original (nom de travail « Turbo Pattes ») : pilotes animaux cartoon, drift qui charge un turbo,
objets ramassés sur la piste, 8 pilotes, 3 tours. Codes du genre, mais personnages, circuits, objets et interface
propres au jeu (pas de reprise de Mario Kart ni d'aucun autre jeu existant).

Dernière mise à jour : 2026-11-01.

Dépôt : https://github.com/YANKAMKEVIN/turbo-pattes (privé, branche `main`). Pousser après chaque tâche.

## Décisions prises (avec Kevin)

| Sujet | Décision |
|---|---|
| Moteur | Unity 6000.6.3f1 + URP 17 + Input System (configuration reprise de `~/dev/check`) |
| Plateformes | Mobile Android **et** PC/manette dès le départ (iOS plus tard) |
| Univers | Animaux pilotes, **tous publics (8-50 ans), pas « bébé »** |
| Direction artistique (2026-11-01) | **« Atelier plein gaz »** : port méditerranéen, rendu peint façon affiche de sport, pilotes athlétiques en combinaison + casque, karts artisanaux en tubes soudés. Palette `#24302D` `#D94D3F` `#E9B949` `#3C8F91` `#F2E7CE`, police Saira (OFL). Fiche : `docs/da/directions-v2/directions.md`, planche `direction-3.png`. Un peu plus coloré et moins réaliste que la planche (accessible comme les grands jeux de kart). Les DA « Club des Pattes » (`docs/da/maquettes-codex`) et l'UI actuelle sont rejetées : trop enfantines. |
| Première étape | Prototype jouable : conduite, drift/turbo, un circuit, IA |
| Identifiant | `com.kevinyankam.turbopattes`, écran en paysage |

## Contenu du prototype

- **Circuit 1** « Prairie des Pattes » (~1,1 km, plat, rayon mini ~15 m) : spline fermée, bitume, vibreurs, murets, portique de départ, tribune, arbres, collines, nuages. Tout est généré par code.
- **Circuit 2** « Canyon Caramel » (~1,18 km, rayon mini ~16 m) : un huit dans un désert de roches rousses. Départ, passage sous le pont, boucle ouest qui ondule (bosse à 5 m, creux, remontée), retour **sur le pont à 8 m** au-dessus du premier passage, plateau, **tremplin** (rampe de 10 m, +1,5 m) au sommet d'une longue descente vers la ligne (~0,9 s de vol). Décor : sable, talus sous la piste surélevée, mesas à strates, cheminées de fée, cactus, rochers, muret terre cuite, ambiance chaude (soleil, brouillard).
- **Circuits 3 à 10** (tracés générés puis vérifiés : rayon mini 16 à 58 m, pente ≤ 8 %, ≥ 8 m de dénivelé aux croisements) :
  « Jungle Papaye » (arbres géants, ruines, 2 tremplins), « Lagon Coco » (îlots de sable, eau, palmiers, voiliers, pont-ponton),
  « Récif Bulle » (sous la mer : coraux, algues, bulles, poissons, brouillard bleu), « Route des Nuages » (piste suspendue entre 30 et 50 m
  au-dessus d'une mer de nuages, îles volantes, montgolfières, gravité 0,85), « Orbite Comète » (huit dans l'espace, étoiles, planètes,
  astéroïdes, gravité 0,55 : sauts de 1,3 s), « Banquise Givrée » (huit avec pont de glace, sapins enneigés, igloos, bonshommes de neige),
  « Volcan Grondant » (montée à 14 m, coulées et mares de lave, volcan fumant), « Ville Néon » (huit de nuit avec échangeur, tours
  aux fenêtres allumées, lampadaires, néons). Ambiance par thème dans `Track/ThemeStyle` (ciel procédural, brouillard, lumière, sol, murets, eau).
- **Relief** : hauteur par point de contrôle (`Circuit.Heights`), reliée par une cubique monotone sans dépassement (`Core/HeightProfile`, avec les tremplins par-dessus, seulement sur bitume + vibreurs). `KartMotor` suit le sol, les côtes freinent et les descentes poussent (45 % de la gravité), il décolle quand le sol se dérobe plus vite que la gravité (`Core/VerticalMotion`, g = 24 m/s²), volant réduit en l'air, tassement et son à la réception, tangage selon la pente. Au croisement, la recherche du tronçon le plus proche tient compte de la hauteur (`Core/TrackProjection`) : tours, IA, objets et chocs entre karts ne confondent pas le pont et la route dessous.
- **Pilotes** (8, modèles en primitives) : Roxy (renarde, joueuse), Bambou (panda), Flip (grenouille), Lili (lapine), Bruno (ours), Mimi (chatte), Pingo (pingouin), Rosa (cochonne). **Caractère** (`Core/DriverStats`) : vitesse, accélération, adhérence, poids de 1 à 5, même total pour tous ; effets de quelques pour cent (pointe ±3 %, accélération ±16 %, braquage ±12 %, le plus lourd est moins bousculé dans les chocs). Jauges affichées sous le nom dans le choix du pilote.
- **Conduite** arcade sans moteur physique (`KartMotor`) : saut + drift à 3 niveaux (étincelles jaunes, vertes, roses), turbo au lâcher, **figure en l'air** (DRIFT pendant un saut : tonneau, petit turbo si elle est finie avant l'atterrissage, `Core/AirTrick` ; l'IA en fait aussi), poussière et secousse de caméra aux réceptions et aux chocs, vibrations sur téléphone, hors-piste lent, rebond sur les murets, chocs entre karts, super départ (accélérer dans la dernière seconde du compte à rebours), tapis turbo.
- **Objets** (bulles surprise à pattes dorées) : carotte turbo, noix de coco (roule tout droit, rebondit), pot de miel (flaque derrière soi), bulle bouclier, **plume planante** (grand bond plané avec élan, passe par-dessus coco, pelote et miel), **pelote de laine** (file le long de la piste vers le kart juste devant ; jamais tirée par le premier). Tirage selon la place (`ItemRoll`). L'IA garde son bouclier pour parer un projectile, saute à la plume par-dessus le miel, lance la pelote quand quelqu'un est devant.
- **IA** : trajectoire avec corde dans les virages, freinage selon la courbure, drift dans les grandes courbes, évitement du miel, usage des objets, élastique léger.
- **Commandes** : clavier (flèches/WASD/ZQSD, Espace = drift, E = objet, R = rejouer), manette (stick, RT/A, LT/B, RB = drift, X/Y = objet, Start), tactile (glisser à gauche pour tourner, boutons DRIFT / OBJET / FREIN, accélération auto).
- **Menus** (IMGUI, outils communs dans `UI/UiKit`) : écran titre sur fond du circuit survolé, choix du pilote (kart sur plateau tournant, cartes des 8 pilotes, flèches), mode Course / Chrono / Coupe, circuit (sélecteur < nom n/10 >, la piste est reconstruite derrière le menu), tours 1/3/5, difficulté Facile/Normal/Difficile (vitesse des IA), « C'est parti ! ». Choix mémorisés (PlayerPrefs, `App/GameSession`). Pause (bouton ⏸ tactile, Échap/P, Start) : Reprendre / Recommencer / Menu. Résultats : Rejouer / Menu. Souris, tactile, clavier (flèches + Entrée) et manette (croix + A/B).
- **Icônes dessinées par code** (`Visuals/IconCanvas` + `Visuals/Icons`, aucun fichier image) : 4 objets (carotte, noix de coco, pot de miel, bulle) et 8 portraits de pilotes, sur les cartes du menu, le classement, les résultats et l'emplacement d'objet. Planche de contrôle : `docs/icones.png`.
- **Musique composée et jouée par code, une par circuit** : 11 partitions originales notées dans `Core/Tune.cs` (menu + 10 circuits, 16 mesures chacune, 120 à 156 BPM), chacune avec sa tonalité, son accompagnement (`SongStyle` : galop western pour le Canyon, marimba et congas pour la Jungle, calypso au tambour d'acier pour le Lagon, échos et bulles pour le Récif, flûte et clochettes pour les Nuages, synthé en doubles croches pour l'Espace, grelots pour la Banquise, guitare saturée et double grosse caisse pour le Volcan, disco pour la Ville Néon) et son timbre de mélodie (`LeadVoice`). Tests : mesures complètes, notes et accords dans la tonalité, tessiture, fin de boucle sur la tonique ou la quinte, un style différent par circuit. Synthétiseur `Audio/MusicSynth` (50 à 170 ms par morceau sur Mac). `Audio/Jukebox` survit aux rechargements de scène : musique du menu, silence pendant le compte à rebours (le morceau du circuit est calculé à ce moment-là), musique du circuit au « PARTEZ ! », plus basse en pause et à l'arrivée ; un seul morceau de circuit gardé en mémoire.
- **Contre-la-montre** (mode « Chrono ») : **écart avec le meilleur tour** aux quarts de tour et à la ligne (vert/rouge, `Core/LapSplits`, temps intermédiaires sauvegardés avec le fantôme) ; **médailles** bronze/argent/or par circuit d'après le meilleur tour (`Core/Medals`, référence = tour lancé de l'IA), affichées aux résultats et dans le menu ; le joueur seul, sans bulles d'objets ; tour en cours, meilleur tour et record affichés ; records par circuit (temps total par nombre de tours, meilleur tour) dans PlayerPrefs (`GameSession.BestTime/BestLap`). **Fantôme** du meilleur tour (`Core/GhostLap` : une pose tous les 0,1 s, interpolée ; `Race/GhostRunner`) : kart translucide qui rejoue ce tour en même temps que le tour en cours, sauvegardé dans `fantome-N.txt` (stockage de l'appli) quand le record tombe, « Record du tour ! » en course. Résultats : temps des tours (meilleur surligné), total, ancien record.
- **Coupes** (mode « Coupe ») : Coupe Pâquerette (Prairie, Canyon, Jungle, Lagon), Coupe Coquillage (Récif, Banquise, Nuages, Espace), Coupe Braise (Volcan, Ville Néon, Canyon, Espace). Points selon la place 10-8-6-5-4-3-2-1, classement cumulé (`Core/Cup` : égalité départagée par le nombre de victoires, de 2es places…, puis la dernière course). Après chaque course : place, points gagnés et classement cumulé, « Course suivante » ; après la 4e : **podium** sur la piste (trois marches, karts, vainqueur qui sautille, confettis, classement complet). Coupe en cours dans `GameSession.Cup` (perdue en revenant au menu). **Grille** : à partir de la 2e course, le dernier du classement part en tête. **Trophées** or/argent/bronze mémorisés ; une coupe s'ouvre après un podium dans la précédente (« Coupe fermée » sinon). Fanfare au podium (`Songs.Podium`), jingle à l'arrivée (`Songs.Finish`). Sons d'interface (déplacement, valider, retour) dans les menus et la pause.
- **Réglages** (`UI/SettingsScreen`, depuis le titre — bouton, Tab ou Y — et la pause) : volume musique, volume effets, sensibilité tactile, vibrations, qualité (Auto ou imposée). La course se met en pause toute seule quand l'appli passe en arrière-plan.
- **Tutoriel** à la première course (`UI/Tutorial`) : super départ, direction, drift, objet, figure, chacun au moment où il sert ; `-tp-tutorial` le force.
- **Icône de l'application** dessinée par code (`Editor/AppIcon`, patte crème sur dégradé orange, icône adaptative Android) ; version 0.2.0.
- **Interface de course** IMGUI : tour, chrono, place, objet avec roulette, classement, vitesse, compte à rebours, « Mauvais sens ! ».
- **Sons** synthétisés par code (moteur, bips, turbo, chocs, objets).

## Architecture

```
Assets/TurboPattes/
  Core/    logique pure testable (sans UnityEngine) : LapTracker, DriftCharge, ItemRoll, RaceRanking, Tune,
           HeightProfile (relief), VerticalMotion (sauts), TrackProjection (tronçon le plus proche en 3D)
  Game/
    App/      KartApp : point d'entrée (seul objet de la scène), construit le monde (LoadCircuit, ambiance par thème) ; autotest
    Track/    Circuit (données des 2 circuits), TrackPath (spline + hauteur + requêtes), TrackBuilder (maillages et décor),
              TrackBuilder.Relief (terrain à talus, ponts, tremplins, décor du canyon),
              TrackBuilder.Themes (décors des 8 autres thèmes), ThemeStyle (ambiance par thème)
    App/      QualityTuner : qualité adaptative (échelle de rendu, MSAA, HDR, ombres, post), niveau mémorisé
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
# -buildTarget est OBLIGATOIRE (sinon le script refuse) : voir « Pièges connus »
/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath ~/dev/turbo-pattes -buildTarget OSXUniversal -executeMethod TurboPattes.EditorTools.BuildScript.BuildMac -logFile -
/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath ~/dev/turbo-pattes -buildTarget Android -executeMethod TurboPattes.EditorTools.BuildScript.BuildAndroid -logFile -
# Course automatique (le joueur est piloté par l'IA), captures + journal dans le dossier :
# -tp-circuit 1 (Prairie, par défaut) ou 2 (Canyon : en plus, vues fixes du pont et du tremplin, captures saut/pont/dessous,
# nombre de sauts par kart et alerte si un kart passe sous la piste). Dossier relatif = stockage de l'appli (Android).
~/dev/turbo-pattes/Builds/macOS/TurboPattes.app/Contents/MacOS/"Turbo Pattes" -tp-autotest /tmp/tp-autotest -tp-laps 1 -tp-circuit 2
# Les 10 circuits d'affilée (1 tour chacun) : résumé OK/ECHEC, fluidité, sauts ; échoue si course non finie, anomalie ou < 30 i/s
# (tools/autotest-difficultes.sh : les mêmes dans les 3 difficultés, option -tp-difficulty 1|2|3)
~/dev/turbo-pattes/tools/autotest-tous.sh /tmp/tp-tous 1
# Diagnostic de fluidité sur téléphone : -tp-diag affiche les temps CPU/GPU ; -tp-quality N impose un niveau (0 à 4)
# Modes : contre-la-montre (3 tours, records effacés, fantôme vu au tour 2-3) puis une coupe entière jusqu'au podium
~/dev/turbo-pattes/tools/autotest-modes.sh /tmp/tp-modes 2 1   # [dossier] [circuit du chrono] [coupe]
# Options d'autotest : -tp-mode chrono|coupe|course, -tp-cup N, -tp-fresh (efface records et fantôme du circuit)
# Musiques en WAV (deux tours de boucle) pour les écouter (menu + 10 circuits) :
/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath ~/dev/turbo-pattes -buildTarget Android -executeMethod TurboPattes.EditorTools.MusicExport.Export -tp-out Builds/Musique -logFile -
# Planche PNG de toutes les icônes (objets + portraits) :
/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath ~/dev/turbo-pattes -buildTarget Android -executeMethod TurboPattes.EditorTools.IconSheet.Export -tp-out docs/icones.png -logFile -
# Parcours des menus (titre, pilote, choix de Bruno + Canyon, course avec un pot de miel en main, pause) avec captures :
~/dev/turbo-pattes/Builds/macOS/TurboPattes.app/Contents/MacOS/"Turbo Pattes" -tp-menutest /tmp/tp-menutest
# Android (S24) : installer, puis lancer avec le diagnostic (rendu + entrées du joueur chaque seconde dans logcat)
~/Library/Android/sdk/platform-tools/adb install -r Builds/Android/TurboPattes.apk
~/Library/Android/sdk/platform-tools/adb shell am start -n com.kevinyankam.turbopattes/com.unity3d.player.UnityPlayerGameActivity -e unity "-tp-diag"
~/Library/Android/sdk/platform-tools/adb logcat -s Unity | grep TPDiag
```

## Vérifications faites (2026-11-01)

- **20 étapes (réglages, pilotes, objets, figures, médailles, trophées…)** : 89 tests NUnit verts (dont 20 nouveaux : qualité adaptative, caractère des pilotes, figures, plume et pelote, médailles, temps intermédiaires, trophées, grille). Mac : `autotest-tous.sh` 10/10 (42 à 55 i/s), `autotest-modes.sh` OK (chrono : 8 écarts affichés, médaille d'argent, temps intermédiaires sauvegardés ; coupe complète avec grille inversée), **`autotest-difficultes.sh` 30/30** (pilote automatique 1er partout en Facile, 1er à 3e en Normal, 2e à 6e en Difficile). Les IA utilisent plume et pelote et font 1 à 3 figures par course. `-tp-menutest` : réglages et jauges des pilotes. Captures : `docs/capture-reglages-pilotes-tutoriel.png`.
  - **Galaxy A17** : course sur la Jungle avec tutoriel (4 conseils, 38 à 56 i/s), contre-la-montre avec l'ancien fantôme relu (format 1) et écart « +1,68 » affiché à la ligne. Captures : `docs/capture-a17-tutoriel-ecart.png`. Les autres sessions (KeRush, Kanto, Shonen Clash) reprennent parfois le téléphone en plein test : relancer si le journal est incomplet.
  - Non vérifié : vibrations ressenties à la main, réglages manipulés au doigt sur le téléphone, icône sur l'écran d'accueil du téléphone.

- **Musiques, contre-la-montre, coupes** : 69 tests NUnit verts (dont 32 nouveaux : partitions, points et classement de coupe, fantôme). `tools/autotest-tous.sh` : 10 circuits OK (41 à 58 i/s, aucune anomalie), chacun avec sa musique. `tools/autotest-modes.sh` : chrono 3 tours sur le Canyon (records enregistrés, fantôme vu à 3,7 m au tour 3, 1 seul kart), coupe Pâquerette complète (4 courses, points cumulés, podium). `-tp-menutest` : lignes Mode, Chrono (record affiché, difficulté grisée) et Coupe (piste de la coupe derrière le menu). Captures : `docs/capture-modes-mac.png`.
  - **Galaxy A17** : chrono 3 tours sur la Prairie à 56-60 i/s (niveau 2), records enregistrés, fantôme translucide visible à côté du joueur au tour 3 ; course normale OK. Captures adb : `docs/capture-a17-chrono-fantome.png`. Corrigé : en tactile, le bouton pause chevauchait le panneau des records.
  - **Galaxy A17, coupe Braise** (`-tp-autotest coupe -tp-mode coupe -tp-cup 3 -tp-laps 1 -tp-quality 2`) : Volcan, Ville Néon, Canyon, Espace enchaînés (scène rechargée entre les courses), 50 à 60 i/s, chaque musique calculée pendant le compte à rebours, points cumulés justes (égalité Bambou/Bruno à 20 départagée par la 2e place de Bambou), podium affiché. Captures : `docs/capture-a17-coupe.png`. Sur Android, `ScreenCapture` n'écrit pas les captures de l'autotest : passer par `adb exec-out screencap`.
  - Pas encore écouté par une oreille humaine : les 9 nouvelles musiques (export WAV avec `MusicExport`).

- **10 circuits** : `tools/autotest-tous.sh` sur Mac, tous OK (course finie, aucune anomalie ni exception), 38 à 57 i/s, 1 ou 2 sauts par tour selon le circuit. Vues d'ensemble vérifiées. Menu : sélecteur « Canyon 2/10 » vérifié par `-tp-menutest`. 37 tests NUnit verts.
- **Galaxy A17** : Orbite Comète, Route des Nuages et Récif Bulle terminés à 60 i/s (niveau de qualité mémorisé), sauts de 1,25 s dans l'espace. Captures : `docs/capture-a17-espace-ciel-recif.png`, planches Mac `docs/capture-circuits-3-8.png` et `docs/capture-circuits-9-10.png`. Le niveau de qualité ne remonte jamais tout seul (mis à 2 sur l'A17) ; `-tp-quality N` le change.
- **Fluidité Galaxy A17** : le MSAA 4x saturait le GPU (45 ms par image). La qualité adaptative descend d'un cran toutes les 4 s sous 45 i/s ; au niveau 2 (échelle 0,75, sans MSAA) ~17 ms soit ~55-60 i/s. Le décor est fusionné par matériau (Canyon : 881 maillages -> 33 objets).


- **Circuit 2 / relief** : 37 tests NUnit verts (dont 10 nouveaux : profil de hauteur, saut et réception, tronçon au croisement d'un huit, un seul tour compté). Autotest Mac sur le Canyon, 3 tours : terminé, ~60 i/s, chaque kart saute 3 fois (~0,9 s), aucune anomalie ; peloton serré (1,2 s). Prairie : 1 tour sans régression. `-tp-menutest` : choix Canyon, piste reconstruite, course lancée sur le Canyon, choix mémorisé. Construction du Canyon ~0,3–0,55 s sur Mac. Captures : `docs/capture-canyon-pont.png`, `docs/capture-canyon-saut.png`, `docs/capture-menu-circuit.png`.
  - Corrigé en route : le sable recouvrait la route dans les côtes (talus pris sur les points de piste suivants) ; près de la piste le sol suit maintenant l'échantillon le plus proche.
  - **Galaxy A17 (SM-A175F, Mali-G57, Vulkan)**, le S24 n'étant pas branché : `am start ... -e unity "-tp-circuit 2 -tp-autotest at -tp-laps 1 -tp-diag"`. Monde visible (Lit = 5 passes), Canyon construit en 324 ms, course terminée, 8 sauts (~0,85 s), passages sous et sur le pont capturés, aucune anomalie. Planche de captures adb : `docs/capture-canyon-a17.png`. Journal récupéré dans `/sdcard/Android/data/com.kevinyankam.turbopattes/files/at/`.
  - **Fluidité sur l'A17 : ~15 i/s, autant sur la Prairie que sur le Canyon** (limite du téléphone en pleine définition, pas du relief). Le S24 tournait à ~60 i/s sur la Prairie. Reste à vérifier le Canyon sur le S24.

- 16 tests NUnit verts sur le Core.
- Build Mac OK : course autotest de 1 tour sans erreur ni exception, ~60 i/s, joueur (piloté par l'IA) 4e en 42,8 s, peloton serré (~2 s). Captures vérifiées : rendu, HUD, résultats.
- Correction faite après la première course : l'IA coupait la boucle serrée par l'herbe (visée trop lointaine).
- **Galaxy S24 (Vulkan, Xclipse 940)** : jeu affiché et jouable, ~60 i/s. Testé par commandes adb simulées : accélération auto, glisser pour tourner, frein/marche arrière. Capture : `docs/capture-android.png`.
  - Corrigé : le monde était invisible sur Android (seuls ciel et HUD) car `GameObject.CreatePrimitive` échouait, le module physique étant retiré du build. `MeshKit.Prim` utilise maintenant les maillages intégrés.
  - Corrigé : l'accélération auto ne s'appliquait pas tant que `Touchscreen.current` était nul (avant le premier toucher).
  - Corrigé : un kart plaqué contre un muret restait presque arrêté ; maintenant il se redresse et longe le mur (rebond seulement sur vrai choc).
- Menus testés sur Mac (`-tp-menutest`) et sur le S24 avec de vraies tapes adb : titre → Lili → 1 tour → course avec son kart → pause → Menu → titre. Captures : `docs/capture-menu-titre.png`, `docs/capture-menu-pilote.png`.
- Non testé : manette, multi-touch réel (tourner + DRIFT en même temps), bouton OBJET, ressenti du drift par un humain.

## Pièges connus

- Matériaux : toujours passer par `Mats` (clones des gabarits de `Resources`), sinon les shaders URP peuvent manquer dans le build.
- **Builds en ligne de commande : toujours `-buildTarget`.** Si l'éditeur démarre sur la cible macOS et que le build Android change de cible en route, URP filtre les shaders avec les réglages de l'autre plateforme : Lit perd une passe entière et le monde est invisible sur le téléphone (seuls ciel et IMGUI). `BuildScript` refuse maintenant de builder si la cible active n'est pas la bonne. Le diagnostic `-tp-diag` affiche le nombre de passes (Lit = 5 quand tout va bien).
- Tapes tactiles : lire `EnhancedTouch` (`UiKit.Tap`), pas `Touchscreen.wasReleasedThisFrame` qui rate les tapes appuyées et relâchées dans la même image.
- Couleurs IMGUI en espace linéaire : convertir en linéaire (`UiKit.Linear`), sinon délavées. Les icônes stockent directement des valeurs linéaires dans une texture marquée linéaire. Les pastilles (`UiKit.Disc`) passent par une boîte à rayon 100 %.
- Ne jamais utiliser `GameObject.CreatePrimitive` (ni rien qui ajoute un collider) : le module physique est retiré des builds IL2CPP, l'objet ne s'affiche pas sur mobile. Passer par `MeshKit.Prim`.
- Brouillard : la scène enregistrée a le brouillard activé pour que ses variantes de shader ne soient pas retirées du build.
- Hérités de Check/KevChess : « Strip Unused Post Processing Variants » désactivé (sinon écran noir Android) ; Mac = Mono seulement ; un seul éditeur Unity par dossier.

## Refonte DA « Atelier plein gaz » (prioritaire)

Toute l'apparence actuelle (pilotes en primitives, karts en blocs, UI IMGUI arrondie) est à remplacer. Grandes étapes :

1. **Interface** : police Saira (fichier OFL dans le projet), palette, nouveaux composants `UiKit` (fiches crème, repères obliques, surlignage vermillon, chiffres à largeur fixe), puis HUD, titre, choix du pilote/circuit, pause, résultats. *Premier passage fait (branche `claude/ux15`) : Saira Regular/SemiBold/Bold dans `Resources/Fonts` (licence OFL jointe, `.meta` écrits à la main), palette dans `Core/Atelier`, composants `UiKit` (fiche à coin oblique, plaque charbon, bouton, segment, repère oblique, chiffres fixes) appliqués à tous les écrans ; non vu dans Unity.* Commandes tactiles : volant/zone de direction, FREIN, DÉRAPAGE, OBJET avec icône. Icônes d'objets et portraits redessinés dans le nouveau style.
2. **Rendu** : shader URP stylisé (ombres en quelques valeurs, lumière chaude), étalonnage, ciel, matériaux peints ; version allégée pour mobile.
3. **Karts** : châssis tubulaires soudés, moteur, pneus crédibles, numéro et livrée par pilote (code : `MeshKit` tubes, chanfreins, normales).
4. **Pilotes** : modèles 3D athlétiques (combinaison, gants, casque) créés par scripts **Blender** (installé : `/opt/homebrew/bin/blender`) et importés en FBX, avec squelette et quelques animations ; remplaçables plus tard par des modèles achetés ou commandés. Jalon : Roxy + son kart, validé par Kevin, avant les 7 autres.
5. **Décors** : harmoniser les 10 circuits avec la palette ; circuit du port méditerranéen comme vitrine.


Objectif fixé par Kevin (2026-10-31) : **au moins 10 circuits** aux décors variés (ciel, espace, eau, désert, jungle…),
dans l'esprit des grands jeux de karting, mais avec des tracés, décors et noms propres au jeu.

1. Vérifier les 10 circuits sur le S24.
2. ~~Qualité adaptative qui baisse et remonte (`Core/QualityGovernor`, sans va-et-vient), réglage manuel~~ (fait).
3. ~~Fusion du décor~~ (fait).
4. Test de ressenti par Kevin (S24 + manette) : vitesse, drift, côtes, saut, IA.
5. ~~Écran de réglages~~ (fait) ; à restyler avec la DA « Atelier ».
6. ~~Dix circuits~~ (fait). Première tranche des virages relevés et du décor animé (branche `claude/suite`, **non exécutée dans Unity**) :
   `Core/Banking` (testé) incline la piste autour de sa ligne médiane (entrée et sortie douces sur 20 m) ; la Route des Nuages relève de 12° son virage le plus serré (`Circuit.Banks`). Route, vibreurs, murets, tablier et `PointAt` suivent l'inclinaison, le kart prend le roulis, glisse moins en drift (`Banking.SlideFactor`) et l'IA y passe plus vite (`Banking.CornerSpeedRatio`). Réservé aux tronçons portés (piste suspendue, ponts) : le terrain ne suit pas encore. La lave du Volcan palpite (`Core/DecorWaves.LavaGlow`, `Track/DecorMotion` posé après la fusion du décor).
   Reste : **boucles** (la piste devra quitter le plan : tangentes et « droite » 3D, caméra et gravité qui suivent la normale du sol, `TrackProjection` en 3D) ; **passages anti-gravité** (mêmes besoins, plus une normale de piste par échantillon et un repère kart lié au sol) ; **vagues** du Lagon (sommets du plan d'eau déplacés par un `Mesh` dynamique ou un décalage de texture) et **nuages** qui dérivent sur la Route des Nuages (déplacer les groupes de nuages avant la fusion, donc hors `MeshKit.Combine`) ; relevés sur terrain (talus qui suit l'inclinaison).
7. ~~Une musique par circuit~~ (fait) ; à faire : que Kevin les écoute (export WAV) et dise lesquelles retoucher.
8. ~~Plume planante et pelote de laine~~ (fait) ; idées : objet rare pour les derniers.
9. ~~Raccourcis risqués~~ (première tranche, branche `claude/suite`, **non exécutée dans Unity**) : `Core/Shortcut` (testé) ; sur la Prairie, dans le grand virage à gauche du nord-est, le muret intérieur recule de 9 m et laisse une bande de sable (vitesse 0,5 contre 0,55 dans l'herbe) avec un petit tremplin rayé (8 m, +1,2 m) : perdant sans turbo, gagnant avec la carotte. L'IA coupe sous turbo et garde sa carotte pour l'entrée. À faire : vérifier dans Unity (autotest Prairie), régler les valeurs au ressenti, en poser sur d'autres circuits (`Circuit.Shortcuts`).
10. ~~Poussière, secousse, figure en l'air = turbo~~ (fait).
11. ~~Contre-la-montre, fantôme, écarts en direct, médailles~~ (fait) ; idée : fantôme d'un autre pilote.
12. ~~Coupes, déblocage, trophées, grille inversée~~ (fait).
13. ~~Podium, fanfare, jingle d'arrivée~~ (fait) ; à enrichir : animations de victoire (avec les nouveaux pilotes Blender).
14. ~~Caractère des pilotes~~ (fait) ; animations de victoire/défaite avec la refonte des pilotes.
15. Modèles de pilotes plus fins, toujours générés par code.
16. ~~Tutoriel à la première course~~ (fait).
17. ~~Autotest de tous les circuits dans les 3 difficultés~~ (fait : `tools/autotest-difficultes.sh`).
18. Tester manette réelle, multi-touch (tourner + drift) et bouton OBJET sur téléphone.
19. Version iOS (simulateur puis iPhone).
20. Publication : ~~icône par code, version 0.2.0~~ (fait) ; reste captures boutique, nom définitif, AAB signé (clé à créer avec Kevin).
