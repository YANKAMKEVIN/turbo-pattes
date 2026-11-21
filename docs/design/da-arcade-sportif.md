# Direction artistique « Arcade sportif »

Décision de Kevin du 2026-11-21 : le design actuel est jugé **trop enfant**. Il est remplacé par une direction
**arcade sportif** : karts et circuits façon sport mécanique, couleurs franches mais sobres, typographie racing,
HUD épuré, matières métal et carbone. Les **pilotes animaux deviennent des pilotes humains**. Tout est concerné :
menus et HUD, pilotes, circuits et décors, karts et objets.

Ce document est la **référence** de la refonte. Chaque valeur est une **valeur par défaut** que Kevin validera ;
toutes sont regroupées dans du code de données testé (`Core/Brand.cs`, `Core/Roster.cs`, `Core/ItemCatalog.cs`),
pour qu'un changement de décision ne touche qu'un seul fichier.

Ce qui ne change pas : physique, équilibrage, caractère des pilotes (`Core/DriverStats`), contenu des modes, règles
de jeu, mécanique de chaque objet, tracés des circuits. La géométrie (pilotes, karts, circuits) et les icônes restent
**générées par code** ; aucun nouvel asset binaire sauf une police libre (OFL), aucun nom ni logo de vraie marque.

## 1. Principes

1. **Le sport avant le décor** : ce qu'on voit à l'écran doit exister sur un vrai circuit de karting — vibreurs,
   glissières, piles de pneus, tribunes, portique de départ à feux, panneaux sponsors, numéros de course.
2. **Sobriété des couleurs** : un fond carbone, un blanc « piste », un rouge course pour l'action, un jaune signal
   pour l'information, un bleu télémétrie pour le froid. Plus de pastel, plus de rose bonbon, plus de dégradés.
3. **Lisibilité d'abord** : le HUD est un instrument de chronométrage, pas une illustration. Plaques opaques sous le
   texte, chiffres à chasse fixe, contraste mesuré (§ 2.3).
4. **Adultes crédibles** : pilotes en combinaison, casque intégral, gants, proportions adultes ; pas de gros yeux, pas
   de joues roses, pas de sourire dessiné.
5. **Le nom du jeu est provisoire** (§ 8) : il n'apparaît qu'à un seul endroit du code.

## 2. Couleurs

### 2.1 Tokens d'interface (`Core/Brand.cs`, classe `Palette`)

| Token | Valeur | Rôle |
|---|---|---|
| `Carbon` | `#14171B` | Fond, plaques du HUD, encre du texte sur fond clair |
| `Graphite` | `#272C33` | Surfaces secondaires (bandeaux, tribunes, toitures) |
| `Chalk` (blanc piste) | `#F2F4F6` | Fiches, boutons secondaires, texte sur fond sombre |
| `Red` (rouge course) | `#C8211B` | Action principale, sélection, joueur, alertes |
| `Signal` (jaune signal) | `#F2C200` | Libellés du HUD, repères, crans des jauges, turbo |
| `Telemetry` (bleu télémétrie) | `#1A62C0` | Écart favorable, fantôme, bouton OBJET |
| `Steel` (acier) | `#8A939E` | Glissières, métal du décor, texte désactivé sur sombre |

Le rouge et le bleu sont volontairement profonds pour porter un texte blanc lisible ; le jaune reste réservé aux
textes sur fond sombre.

### 2.2 Couleurs d'équipe (une par pilote)

Couleur principale (casque, carrosserie du kart) et couleur secondaire (bande du casque, liseré, jantes). Huit
couleurs franches, toutes différentes, choisies pour se distinguer de dos à 30 m :

| N° | Pilote | Principale | Secondaire |
|---|---|---|---|
| 7 | Alex Moreau | rouge `#C8211B` | blanc `#F2F4F6` |
| 12 | Viktor Brandt | vert course `#1E7A46` | jaune `#F2C200` |
| 23 | Lina Okafor | jaune `#F2C200` | carbone `#14171B` |
| 31 | Sami Haddad | orange `#E5711B` | carbone `#14171B` |
| 5 | Teo Ferraz | bleu `#1A62C0` | blanc `#F2F4F6` |
| 18 | Hana Mori | blanc `#E9ECEF` | rouge `#C8211B` |
| 27 | Nora Lindqvist | cyan `#1497A8` | blanc `#F2F4F6` |
| 64 | Inès Duval | violet `#5B3A9C` | jaune `#F2C200` |

### 2.3 Contraste du HUD

Chaque couple « texte sur fond » réellement utilisé par le HUD et les menus est listé dans `Palette.TextPairs` et
testé (ratio de contraste WCAG 2.1) : **4,5:1 minimum** pour le texte courant, **3:1** pour les gros titres et
bandeaux (corps ≥ 48 px à 720 p). Les plaques du HUD sont presque opaques (86 %) pour que ce contraste tienne quel
que soit le décor derrière.

## 3. Typographie

- **Saira Condensed** Bold et SemiBold (Omnibus-Type, licence SIL OFL 1.1, `Resources/Fonts/SairaCondensed-OFL.txt`) :
  titres, chiffres, libellés du HUD, boutons. Le dessin carré et serré de Saira est celui des numéros de course et
  des écrans de chronométrage.
- **Saira** Regular (déjà présente) : phrases d'aide et descriptions, plus confortables en largeur normale.
- Libellés du HUD **en capitales** (« TOUR », « POS », « DEVANT »). Phrases en casse normale.
- Chiffres à **chasse fixe** (`FixedDigits`) : cases recalculées pour la version condensée (chiffre le plus large :
  0,49 em).
- Si les polices manquent (import raté), la police système prend le relais en gras.

## 4. HUD de course

Disposition (paysage, coordonnées pour 1280 × 720, mise à l'échelle sur la hauteur, zones sûres respectées) :

| Zone | Contenu |
|---|---|
| Haut gauche | Plaque carbone : `TOUR 2/3` (libellé jaune, chiffres blancs) et chrono de course `1:05,32` ; en contre-la-montre, tour en cours et fiche des records |
| Haut droite | **Position** en très gros chiffres sur fiche (rouge quand on est 1er, blanc sinon) et « sur 8 » |
| Gauche, sous le chrono | **Tour de chronométrage** : écarts avec le pilote devant et derrière (code à trois lettres, écart en secondes, bleu si l'écart évolue en notre faveur, rouge sinon) ; le classement complet s'affiche 2 s à chaque passage de ligne |
| Bas gauche | **Mini-carte** : tracé blanc fin, ligne de départ jaune, points des karts carbone, joueur blanc cerclé |
| Bas centre | Vitesse en chiffres fixes, `km/h` en petit ; éclat jaune au déclenchement d'un turbo |
| Haut centre | Emplacement d'objet (pastille carbone cerclée de blanc) et nom de l'objet en capitales |
| Centre | Seulement le compte à rebours, les bandeaux (« PARTEZ ! », « Dernier tour ! ») et l'alerte « Mauvais sens ! » |

Règles : rien de décoratif au centre de l'écran, pas de portrait animé pendant la course, une seule couleur
d'alerte (rouge). Les commandes tactiles restent des disques carbone translucides avec un libellé en capitales.

## 5. Karts

- **Carénage** : nez avant enveloppant, pontons latéraux, panneau frontal porte-numéro, spoiler arrière bas ;
  plancher et pare-chocs en **carbone** (gris très sombre brillant), échappements et axes en **métal**.
- **Livrée** : couleur principale de l'équipe sur le carénage, couleur secondaire en bande longitudinale et sur les
  jantes, liseré carbone.
- **Numéro** : plaque blanche à chiffres carbone sur le nez et les pontons, dessinée par code (chiffres à segments,
  `Icons.NumberPlate`).
- **Roues** : pneus slicks noirs, jantes métal à cinq branches suggérées, pas de couleurs vives sur les roues.

## 6. Pilotes humains

Huit pilotes, **mêmes caractères de jeu** qu'avant, dans le même ordre (le n° 0 reste le pilote par défaut) :

| Ancien | Nouveau | Code | N° | Caractère (inchangé) |
|---|---|---|---|---|
| Roxy | **Alex** Moreau | MOR | 7 | équilibré(e) — 3/3/3/3 |
| Bambou | **Viktor** Brandt | BRA | 12 | lourd, inébranlable — 3/2/2/5 |
| Flip | **Lina** Okafor | OKA | 23 | agile — 2/4/4/2 |
| Lili | **Sami** Haddad | HAD | 31 | démarrages fulgurants — 2/5/3/2 |
| Bruno | **Teo** Ferraz | FER | 5 | pointe et poids — 4/1/2/5 |
| Mimi | **Hana** Mori | MRI | 18 | précise — 3/3/4/2 |
| Pingo | **Nora** Lindqvist | LIN | 27 | glisse vite — 4/3/2/3 |
| Rosa | **Inès** Duval | DUV | 64 | solide — 3/2/3/4 |

(Le code de Hana est **MRI** pour ne pas doubler celui d'Alex.) Noms neutres et internationaux, mixtes, sans
référence à un pilote réel connu ; prénom affiché dans les menus et le HUD, code à trois lettres dans le classement
compact.

Modèle (primitives générées par code, `KartFactory.BuildDriver`) :
- **proportions adultes** : tête (casque compris) ≈ 1/6,5 de la hauteur assise, buste plus large que le casque,
  épaules marquées, bassin et cuisses visibles dans le baquet, jambes allongées vers le pédalier ;
- **combinaison** une pièce à la couleur de l'équipe, bande secondaire sur le buste, col montant ;
- **casque intégral** à la couleur principale, bande de la couleur secondaire, **visière fumée** brillante,
  mentonnière ; jamais de visage visible ;
- **gants** carbone, mains posées sur le volant ;
- portraits (icônes) : casque de trois quarts avec visière et bande, sur pastille de la couleur d'équipe.

Animations de fin de course (inchangées dans leur logique, `Core/DriverPoses`) : victoire = bras levés, poing qui
salue, petits sauts, tête vers le ciel ; défaite = mains qui lâchent le volant, tête basse qui se secoue, buste
tassé. Elles s'appliquent au nouveau squelette (épaules et tête articulées) ; l'amplitude est réduite de 20 % sur les
bras pour rester crédible sur des proportions adultes.

## 7. Objets : même mécanique, habillage sport mécanique

| Identifiant (code) | Avant | **Après** | Mécanique (inchangée) |
|---|---|---|---|
| `Carrot` | Carotte turbo | **Nitro** (bouteille) | Accélération immédiate |
| `Coconut` | Noix de coco | **Pneu fou** | Roule tout droit, rebondit sur les murets, fait tourner le kart touché |
| `Honey` | Pot de miel | **Flaque d'huile** (bidon renversé) | Flaque posée derrière soi, fait tourner |
| `Shield` | Bulle bouclier | **Bouclier carbone** | Absorbe un coup pendant quelques secondes |
| `Feather` | Plume planante | **Aileron** | Grand bond plané avec élan, passe au-dessus des objets au sol |
| `Yarn` | Pelote de laine | **Drone traqueur** | File le long de la piste vers le kart juste devant |
| `Comet` | Comète | **Aspiration** | Objet rare des derniers : turbo en pilote automatique, intouchable, renverse les karts percutés |

Les identifiants de code (`ItemKind.Carrot`…) sont **gardés** : ils nomment une mécanique, et les garder garantit
qu'aucune règle ne bouge (tirage, IA, sauvegardes). Seuls l'affichage (`ItemCatalog.Name`) et les visuels changent.

Bulles d'objets → **caisses objet** : cube translucide qui tourne, chevron jaune lumineux au centre.

## 8. Circuits et décors

- **Vibreurs** rouge et blanc (déjà présents), **glissières** acier à bandes blanches et rouges selon le thème,
  **piles de pneus** aux points de freinage, **tribune** couverte graphite avec public aux couleurs des équipes,
  **panneaux sponsors fictifs** le long de la ligne droite et sur le portique.
- **Portique de départ** : poutre carbone avec **cinq feux rouges** et bandeau damier ; plus de pattes dorées.
- **Palette des décors** : chaque thème garde son ambiance (ciel, lumière, sol), mais les murets passent à des
  couleurs sport (acier, blanc, rouge, ou couleurs de l'univers en version désaturée).
- **Sponsors fictifs** (inventés, sans logo réel ; à vérifier avant publication) : VELTRA (pneus), KORVEX (huiles),
  ORLAN (énergie), TAVIK (freins), MIRALTO (chronométrage). Texte en police pixel générée par code
  (`Core/PixelFont`).

Noms de circuits : on ne change que les noms puérils.

| Avant | **Proposition** |
|---|---|
| Prairie des Pattes | **Circuit des Prés** |
| Canyon Caramel | **Canyon Rouge** |
| Jungle Papaye | **Jungle Émeraude** |
| Lagon Coco | **Lagon Turquoise** |
| Récif Bulle | **Récif Profond** |
| Route des Nuages, Orbite Comète, Banquise Givrée, Volcan Grondant, Ville Néon | inchangés |

Coupes : « Coupe Pâquerette » → **Coupe Asphalte**, « Coupe Coquillage » → **Coupe Horizon**, « Coupe Braise »
inchangée. Les noms courts (Prairie, Canyon…) et les musiques ne changent pas (les musiques n'ont pas encore été
écoutées par Kevin).

## 9. Nom du jeu

« Turbo Pattes » ne colle plus à des pilotes humains. Le nom **n'est pas changé** dans le dépôt, le projet Unity ni
l'identifiant Android ; le titre affiché vient d'une seule constante, `Brand.Title` (le logo coupe le titre en deux
lignes). Trois propositions pour Kevin (disponibilité des marques à vérifier) :

1. **Apex Kart** — court, international, dit la trajectoire idéale.
2. **Ligne Rouge** — le compte-tours à fond, francophone, se décline en « Ligne Rouge Karting ».
3. **Turbo Grid** — garde « Turbo » et la mémoire du nom actuel, évoque la grille de départ.

Accroche par défaut sous le logo : « Championnat de karting arcade ».

## 10. Limites et suite

- Unity n'est pas installé dans le conteneur de la refonte : rien n'a été vu à l'écran. Valider sur Mac : `-tp-menutest`,
  `autotest-tous.sh`, planche d'icônes (`IconSheet`).
- Les pilotes restent en primitives ; des maillages modelés (`MeshKit`) ou des modèles achetés amélioreraient
  visages de casque et mains.
- L'icône de l'application (patte) est redessinée en chevron et damier (`Editor/AppIcon`).
- Les anciennes maquettes « Atelier plein gaz » et « Club des Pattes » (`docs/da/`) sont conservées pour l'historique,
  elles ne sont plus la référence.

## À valider par Kevin

- Le nom du jeu (§ 9) et l'accroche.
- La palette (§ 2) et les couleurs d'équipe.
- Les noms, numéros et codes des pilotes (§ 6).
- Les noms des objets (§ 7), des circuits et des coupes (§ 8), les sponsors fictifs.
- La police Saira Condensed (§ 3).
