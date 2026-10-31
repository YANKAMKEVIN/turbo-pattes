# Avis de Codex sur la DA de l'interface (2026-11-01)

Demandé sur les captures titre, choix du pilote et HUD Android. Base de travail pour les maquettes Figma.

## 1. Diagnostic — les problèmes, par ordre d’importance

**Le propriétaire a raison sur le manque de cohérence.** Les animaux et les karts donnent une base sympathique ; l’interface, elle, ressemble encore à un assemblage de commandes de prototype. Changer seulement la police ne suffira pas.

1. **La hiérarchie visuelle est mal répartie.** Dans le choix du pilote, les flèches, les réglages et le bouton de départ concurrencent le personnage. En course, les commandes tactiles prennent beaucoup de place, tandis que l’objet détenu reste loin du pouce qui doit l’utiliser.

2. **La lisibilité dépend trop du décor.** Blanc avec ombre grise sur ciel, bitume ou herbe : cela fonctionne par endroits, puis se dégrade. Les panneaux transparents laissent également varier le contraste. Il faut une protection visuelle stable derrière les informations essentielles.

3. **Les composants parlent plusieurs langages.** Boutons brillants orange, capsules bleu-gris, disques translucides, cadres jaunes : chacun pourrait appartenir à un jeu différent. Aucun motif ne signe encore « Turbo Pattes ».

4. **Le HUD ressemble à une superposition de toutes les informations disponibles.** Huit concurrents affichés en permanence, chrono, tours, position, vitesse, objet et commandes : tout réclame de l’attention. La conduite devrait rester le centre de l’écran.

5. **Les états de sélection sont insuffisamment explicites.** Le bleu signifie « sélectionné » dans les réglages ; le jaune remplit ce rôle pour Bruno. Le reflet clair ressemble parfois à une barre de progression. Il manque une règle commune pour repos, sélection, pression et focus manette.

6. **L’écran titre manque de composition et de personnalité.** Le circuit est accueillant, mais le logo paraît posé dessus. Sa typographie fine et son ombre ne portent pas l’énergie du nom. La promesse animale se trouve surtout dans le sous-titre.

7. **Le tactile semble dimensionné par l’espace disponible plutôt que par le geste.** Les trois disques droits sont dispersés ; « < glisser > » ne montre pas clairement où poser le pouce ni comment la direction répond. Il faut concevoir une prise en main, puis dessiner ses commandes.

---

## 2. Direction artistique proposée

### **« Club des Pattes »**

Une compétition joyeuse organisée par une bande d’animaux : badges de pilote, plaques épaisses, marques de trajectoire et accents de couleur francs.  
Une interface chaleureuse et sportive, avec une signature récurrente : **deux petites encoches arrondies évoquant des traces de pattes**, utilisées avec parcimonie.

L’identité doit venir de ce système graphique, des portraits existants et du ton des textes. Éviter l’accumulation de damiers, de pièces et de codes immédiatement associés à d’autres jeux de kart.

### Alternative contrastée : **« Atelier Turbo »**

Un univers de petits bolides bricolés : plaques crème, attaches dessinées, compteurs simples et aplats d’encre sombre.  
Plus artisanal et malicieux, moins sportif ; intéressant si les karts et les circuits évoluent vers un monde fait maison.

**Je choisirais « Club des Pattes »** : il accompagne aussi bien la prairie que l’espace ou le canyon, sans imposer un matériau à tous les environnements.

---

## 3. Palette, police et formes

### Palette principale

| Couleur | Hex | Rôle |
|---|---|---|
| Encre aubergine | `#29263D` | Texte, contours, fonds du HUD |
| Crème papier | `#FFF4DF` | Panneaux de menu, texte sur fond sombre |
| Mandarine | `#FF8738` | Action principale : jouer, partir, continuer |
| Miel | `#FFD65A` | Sélection, focus, récompense |
| Menthe | `#65D5BA` | Validation, confirmation, information positive |
| Bleu pervenche | `#8CA9EF` | Actions secondaires, repères de commande |
| Framboise | `#D94E70` | Erreur, abandon, alerte ponctuelle |

**Règle importante :** orange = action ; miel = sélection. Les couleurs des huit pilotes restent contenues dans leurs portraits et leurs petits accents personnels.

Sur orange, miel et menthe, employer le texte aubergine. Réserver le crème aux surfaces sombres.

### Police

**Recommandation : Baloo 2**, en SemiBold et ExtraBold. Ses formes rondes donnent de la personnalité sans nécessiter d’illustrations supplémentaires. Utiliser ExtraBold pour titres et chiffres importants, SemiBold pour les commandes. Elle est distribuée sous licence OFL. [Source officielle Ek Type](https://github.com/EkType/Baloo2)

**Secours : Nunito Sans**, plus sobre et compacte, également OFL. Ce serait un remplacement de la famille principale, pour respecter votre contrainte d’une seule police. [Métadonnées officielles Google Fonts](https://github.com/google/fonts/blob/main/ofl/nunitosans/METADATA.pb)

Tester avant validation : `8e`, `1/3`, `00:38,52`, « Difficulté », « C’est parti ! ». Réserver une largeur fixe au chrono pour éviter les déplacements des chiffres.

### Langage de formes

- **Boutons :** rectangles arrondis trapus, rayon voisin du quart de leur hauteur. Pas de capsules partout.
- **Panneaux :** crème opaque dans les menus ; aubergine presque opaque en course.
- **Contours :** aubergine, réguliers, environ 2–3 unités pour une maquette de 900 unités de haut.
- **Ombres :** une seule ombre courte, décalée vers le bas, sans flou indispensable.
- **Relief :** tranche inférieure de 4 unités ; supprimer les grands reflets blancs actuels.
- **Sélection :** liseré miel + petit repère plein + changement de fond. Elle doit rester identifiable sans la couleur.
- **Angles :** composants droits ; inclinaison de 3–4° réservée à un badge de victoire ou au logo.

Tout cela se construit avec rectangles arrondis, disques et quelques masques SDF.

---

## 4. Écran par écran

### Écran titre

**Faire du titre et des animaux le souvenir de cet écran.**

- Logo dans le tiers supérieur, plus compact : « TURBO » au-dessus de « PATTES », lettres épaisses crème, contour aubergine, accent mandarine.
- Ajouter deux petites marques de trajectoire sous « TURBO », plutôt qu’une grosse décoration de course.
- Garder le circuit vivant en arrière-plan, avec un voile aubergine léger derrière la zone du logo uniquement.
- Composer le plan avec deux ou trois pilotes visibles, même petits, plutôt qu’un circuit essentiellement vide.
- Un seul grand bouton **« Jouer »**, sous le logo.
- Réglages et son dans un coin inférieur, en petites commandes identifiables.
- Afficher l’invite correspondant au périphérique actif : toucher, Entrée ou bouton de manette. Éviter « Entrée ou A » pour tout le monde.

Le sous-titre actuel est sympathique, mais il doit rester secondaire et ne pas toucher visuellement le logo.

### Choix du pilote

**Transformer l’écran en présentation de pilote, avec des réglages regroupés.**

Disposition de départ :

- **60 % à gauche :** personnage et kart, nom au-dessus, flèches proches du modèle.
- **35 % à droite :** une fiche crème « Ta course », regroupant tours et difficulté.
- **Bande inférieure gauche :** les huit portraits.
- **Bas droit :** bouton « C’est parti ! », aligné sur la fiche.

Idées fortes :

- Réduire le socle : actuellement, il prend presque autant d’importance que le kart.
- Donner à chaque pilote une courte signature : **« Bruno — Toujours partant »**. Éviter des statistiques si elles ne correspondent pas à de vraies différences de conduite.
- Portrait sélectionné légèrement remonté, cadre miel et petit onglet « Ton pilote ».
- Réglages sous forme de segments intégrés à une même barre, sans effet brillant.
- Sous la difficulté, une explication concrète correspondant au comportement réel : rythme des adversaires, aides éventuelles.
- Sur PC et manette, rendre le focus aussi net que la sélection : le joueur doit distinguer « Bruno est choisi » de « je suis actuellement sur le réglage Tours ».

### HUD en course

**Objectif : regarder la route et retrouver les commandes au toucher.**

| Élément | Proposition |
|---|---|
| Position | En haut à gauche, plaque sombre compacte : grand `8e`, petit `/ 8` |
| Tours | Juste à côté : `TOUR 1/3`, taille secondaire |
| Chrono | Sous les tours, discret ; davantage valorisé dans un mode chronométré |
| Classement | Afficher le joueur et ses voisins immédiats ; classement complet au départ, en pause et à l’arrivée |
| Objet | Directement intégré au bouton d’utilisation à droite |
| Vitesse | Petite capsule basse, légèrement décalée du kart ; priorité faible |
| Pause | Coin supérieur droit, dans la zone sûre de l’écran |

**Commandes tactiles :**

- À gauche, une **zone de direction large et tolérante**, avec un petit disque qui suit le déplacement horizontal. Au repos, deux chevrons suffisent ; enlever le texte permanent « glisser » après l’apprentissage.
- À droite, **Drift** en bouton principal bas ; **objet** juste au-dessus ; **frein** plus petit, à l’intérieur de cet ensemble, sans chevauchement des zones tactiles.
- Le bouton d’objet contient le pictogramme détenu. Son nom apparaît brièvement à l’acquisition, puis disparaît.
- Conserver les centres des commandes stables pendant la course. Agrandir la zone de toucher au-delà du dessin.
- Viser au moins **48 dp de cible tactile**, davantage pour direction et drift ; vérifier sur téléphone tenu à deux mains.
- Proposer ensuite réglage de taille et léger déplacement des commandes.

**Lisibilité sur décor clair :** plaques aubergine presque opaques derrière les chiffres ; texte crème ; contour sombre autour des icônes. Ne pas compter sur une ombre de texte seule. Tester également devant la neige, le ciel et des effets lumineux.

Sur PC, retirer les grandes commandes tactiles et garder des indications de touches compactes, visibles surtout au début ou après changement de périphérique.

### Pause

- Voile sombre sur la scène figée ; pas besoin d’un flou.
- Panneau central crème : **« Petite pause »**.
- « Reprendre » dominant, puis « Recommencer », « Réglages », « Quitter la course ».
- Confirmation pour quitter ou recommencer lorsque cela fait perdre la course en cours.
- Focus initial sur « Reprendre » ; retour manette/clavier cohérent.
- Réglages accessibles sans perdre la sélection ni la progression dans le menu.

### Résultats

**Célébrer la performance de chacun, même en dernière position.**

- À gauche : pilote et kart, rang dans un grand badge.
- À droite : classement lisible avec ligne du joueur accentuée.
- Sous le rang : temps total et meilleur tour.
- Une seule distinction personnelle, fondée sur une mesure réelle : **« Ton meilleur tour ! »**, par exemple.
- Deux actions : « Rejouer » et « Changer de pilote ». Leur importance dépend du parcours choisi pour la suite du jeu.
- Pour la victoire, quelques confettis faits de disques et de tirets ; pas de pluie permanente derrière les résultats.

---

## 5. Micro-animations et retours

| Événement | Animation réalisable par code | Son |
|---|---|---|
| Pression sur un bouton | Descente de 3 unités, ombre raccourcie, 70–100 ms | Petit « toc » mat |
| Changement de pilote | Portrait relevé, cadre qui se déploie en 150 ms | Note courte et chaude |
| Arrivée d’un panneau | Translation de 12 unités + fondu, 180–220 ms | Souffle très discret |
| Acquisition d’objet | Icône de 85 à 110 puis 100 %, en 220 ms | Deux notes ascendantes |
| Objet prêt | Une seule pulsation du contour | Signal bref |
| Changement de position | Chiffre entrant verticalement, 150 ms | Son seulement si gain significatif |
| Validation du départ | Bouton comprimé puis transition | Petit motif de trois notes |

Le rebond convient aux portraits et récompenses ; garder les chiffres de course stables. Prévoir une option de mouvements réduits. En IMGUI, calculer ces états à partir du temps et des événements, sans relancer l’animation à chaque passage de dessin.

---

## 6. Ce qu’il faut maquetter en priorité dans Figma

**Commencer par le HUD, car c’est l’écran le plus contraint et le plus longtemps regardé.**

1. **HUD sur la capture réelle**, puis sur un décor très clair : version téléphone large et version 16:9. Vérifier les zones couvertes par les pouces, les encoches et la silhouette du kart.
2. **Une planche de composants** : bouton principal, secondaire, segment, portrait, plaque HUD ; états repos, pressé, sélectionné, focus et désactivé.
3. **Choix du pilote**, avec le nom le plus long, les huit portraits et la navigation manette matérialisée.
4. **Titre**, une fois les composants validés : travail spécifique sur le logo et la composition.
5. **Pause et résultats**, pour vérifier que le système produit plusieurs écrans cohérents.

Dans Figma, imposer dès le départ les primitives disponibles en code. **La première maquette à valider serait un HUD posé sur votre course actuelle : si la route respire, que l’objet se comprend et que les pouces trouvent leurs commandes, la refonte aura déjà résolu le problème principal.**
