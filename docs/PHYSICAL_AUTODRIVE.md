# Conduite physique — branche de développement

Base : VoogleRoute **1.0.11**, commit `45d97b0`, sur **Big Ambitions 1.0 Build 3682**
(Steam build 25482473, Unity 2022.3.62f2). Branche : `codex/physical-autodrive`.
Le build local courant se distingue par `AssemblyInformationalVersion = 1.0.11-physical-autodrive.5`.

## Utilisation

1. Dans Échap → Options → Mods → Voogle Route, laisser **Conduite réelle** activée.
   **Vitesse de croisière automatique** est activé par défaut : cible proche de
   50 km/h en ville, réduite pour les virages, feux, obstacles et capacités du véhicule.
   Désactiver ce réglage permet de choisir un plafond manuel entre 20 et 80 km/h
   (40 par défaut).
   **S'arrêter pour les piétons** est activé par défaut.
   **Dépasser les limites (max. 80 km/h)** retire le plafond légal : le mode
   automatique vise alors 80 km/h, le mode manuel garde le plafond choisi.
   **Griller les feux rouges** permet de franchir un feu rouge, en continuant à
   surveiller les véhicules qui traversent. Ces deux dérogations sont désactivées
   par défaut. Sans dépassement autorisé, le plafond légal reste appliqué.
   Ces réglages sont enregistrés dans les options de la sauvegarde.
   Désactiver Conduite réelle rétablit le voyage rapide existant.
2. Monter dans une voiture, rejoindre une voie de circulation et s'arrêter dans
   son sens de circulation. Un départ depuis une place de parking n'est pas prévu.
3. Choisir une destination, demander AUTO-DRIVE puis confirmer.
4. Les touches de déplacement, l'interaction de sortie, Échap ou ARRÊTER rendent
   immédiatement la main. À l'arrivée sur la voie, stationner manuellement.

## Implémentation

- `VehicleControlLease` lit l'empattement des vraies roues NWH et prend seulement
  les entrées de la voiture occupée. Les réglages `autoSetInput`,
  `swapInputInReverse`, `useRawInput`, l'assistance externe et le délégué de passage
  des rapports sont restaurés. Les rapports montent et descendent selon le régime.
- `PhysicalRoutingGraph` filtre les demi-tours et transitions interdites, et préfère
  les raccords larges. Le départ appartient à une voie orientée comme le véhicule.
- `PhysicalDrivePlanner` utilise A* et le graphe routier existant. Il conserve les
  ancrages de voie, lisse les courbes complètes et vérifie la courbure. Aucune
  corde finale vers le bâtiment n'est ajoutée. Une route incompatible est refusée.
- `DrivingPath` suit une progression locale monotone, avec visée depuis l'essieu
  arrière et profil de vitesse anticipant virages, limites de voie et arrêt final.
- `DrivingSpeedLimits` reprend la politique routière 50/80 km/h et le CSV de zones
  de BetterFines, sans dépendance au DLL de ce mod. Le profil anticipe les baisses
  de limite et tient compte du maximum du véhicule et de son limiteur natif.
- `DrivingTrafficSensor` relie les positions du graphe aux waypoints Gley **vivants**,
  puis lit `stop`, `giveWay` et `CanChange`. Des balayages de
  boîte suivent le prochain tronçon pour adapter la vitesse aux obstacles et voitures.
- `PhysicalDriveService` exécute la commande en FixedUpdate, laisse le jeu gérer
  carburant et dommages et freine avant de rendre la main après un blocage prolongé.
  Il libère les entrées à la sortie, au changement de destination, de sauvegarde ou
  de scène, à la désactivation du pilote et au déchargement du mod.

Le pilote physique ne téléporte pas la voiture et ne modifie pas l'horloge du jeu.
Le voyage rapide conserve son propre service et sa confirmation.

## Construction et installation locale

Depuis ce dossier de mod, en PowerShell :

```powershell
./tools/build-local-physical.ps1 -Install
```

Le script reprend les références Player du SDK et du jeu installé, compile sans
symboles de débogage, construit les 33 fichiers du paquet dans `Output/VoogleRoute`
puis les recopie dans `ModsLocal/VoogleRoute`. Il refuse une installation pendant
que le jeu tourne, préserve les fichiers utilisateur et compare les empreintes de
tous les fichiers livrés. Les seules dépendances sont celles déjà requises par le
mod ; la bibliothèque Pathfinding reste inchangée.

## Preuves et limites

Compilation du code complet avec les DLL du jeu installé : réussie. Profil Player
contrôlé. Quatre avertissements préexistants concernent `TMP_Text.enableWordWrapping`.
Les fichiers du paquet et les fichiers utilisateur sont contrôlés lors de l'installation.

Installation révision 5 du 16 octobre 2026 : 153 sources compilées, 33 fichiers livrés et comparés.
Empreinte SHA-256 du DLL installé :
`EFEF58BEC164581DDA90C89FB9BC74A5397B6E0805D4642BC05A017BF97D231A`.
L'unique fichier utilisateur présent, `config.json`, conserve son empreinte
`59505653560F93D6E4A8273782B463001FB89049415019B37A877A04BE7EF07D`.

L'utilisateur a essayé la révision 3 et indique que la conduite semble bonne.
**Les nouveaux réglages et comportements de la révision 5 restent à observer en
jeu, ainsi que les rapports introduits en révision 4.** La compilation et les empreintes ne constituent
pas une mesure du comportement physique.
Il n'y a pas de marche arrière, de stationnement automatique ni de récupération par
téléportation. Les voies élevées de pont et souterraines sont exclues du graphe de
conduite de cette première version. Les trajets sans voie compatible sont refusés ; le voyage rapide reste
disponible dans les options. Aucune publication Steam de cette branche n'est effectuée.

## Révision 2 : refus malgré une ligne au sol

Le Player.log de la session du 16 octobre (démarrée à 14:14) ne contient pas
d'exception du pilote. Les refus normaux n'étaient pas journalisés et le niveau
de log du mod était `error`, donc ce fichier ne permet pas d'identifier le garde
exact déclenché lors de l'essai de l'utilisateur.

Une reproduction depuis la zone de voiture observée dans les logs, autour de
`(-208.2, 0, -80)`, démontre néanmoins un défaut : A* trouve des routes, mais
l'ancien `TryRoundCorners` contraint le rayon au plus petit segment de waypoint
adjacent, multiplié par 0.45. Au nœud 8499, les segments de 4.43 m et 3.22 m avec
un angle de 70.8° limitent artificiellement le rayon à 2.04 m. Aucun détour n'était
essayé après le refus de cette géométrie.

La révision 2 utilise `DrivingPathGeometry` : rééchantillonnage régulier, relaxation
de la courbe complète dans un couloir borné à 1.8 m, puis contrôle du rayon réel.
`DrivingRouteGeometry` essaye jusqu'à huit routes en excluant les raccords dont la
trajectoire reste incompatible. Les tangentes des courbes synthétiques remplacent
les cordes pour contrôler les raccords ; leur courbure brute devient une pénalité,
avec décision de faisabilité sur la trajectoire lissée. Le choix du côté d'arrivée
respecte désormais le réglage utilisé pour le trait au sol.

Rejeu hors jeu des sources de routage et de géométrie : origine ci-dessus, premier
nœud 11223, cibles `(-266, -70)`, `(0, 0)`, `(100, -200)`, rayons de 3, 5 et 7 m :
**neuf trajectoires calculées sur neuf**. Les détours demandent de zéro à trois
recherches supplémentaires. Les rayons et destinations sont des cas de diagnostic,
pas une capture de la requête utilisateur, qui n'était pas enregistrée.

Chaque demande écrit désormais dans Player.log, même lorsque le fichier de log
optionnel du mod est désactivé : `plan_request`, paramètres de voiture, origine,
cible, `curve_fit_ok`, `plan_ok` ou `plan_rejected stage=...`, avec présence du
trajet affiché. Une géométrie trop serrée possède son propre message localisé.
Le trajet réel de l'utilisateur reste à refaire en jeu avec cette révision.

## Révision 3 : fluidité et mesures du pilote

- La confirmation réutilise le plan préparé si voiture, position, cap, géométrie,
  destination, sauvegarde, scène, graphe, source de trafic et options restent valides.
  Le plan expire après 60 secondes ; un plan périmé est recalculé une fois. Le carburant
  est revérifié au départ. Les contrôles de validité restent sur le thread principal.
- La projection sur la trajectoire commence par une recherche binaire autour de la
  progression, suivie de la fenêtre locale de 18 m. Elle ne reparcourt plus tous
  les points déjà dépassés. Les marqueurs de trafic dépassés utilisent un curseur.
- La grille des waypoints vivants et les correspondances avec les nœuds routiers
  sont conservées par contexte de sauvegarde/scène/graphe/source et libérées au
  déchargement. Le rattachement d'un marqueur à la trajectoire utilise une grille
  spatiale, avec repli exact hors du voisinage attendu.
- Les composants des colliders sont conservés pendant un trajet dans un cache
  borné à 512 entrées. Leur vitesse est toujours relue ; les doublons d'obstacles
  d'un même balayage sont éliminés. Le cache est vidé à l'arrêt du pilote.
- Les balayages proches et les courbes restent découpés à 4 m. Au-delà de 8 m,
  les portions rectilignes vérifiées sur tous les points intermédiaires peuvent
  être regroupées jusqu'à 12 m, avec une boîte élargie. La fréquence de 0.08 s,
  les layers, la détection des feux/priorités et les distances de freinage restent
  les mêmes. La saturation d'un résultat PhysX impose toujours l'arrêt.
- Le calcul de vitesse cible n'alloue plus de tableau via `Mathf.Min(params)`
  à chaque tick physique.

### Lecture des nouveaux logs

Une ligne `[VoogleRoute][Perf]` est écrite toutes les cinq secondes dans Player.log,
même avec `logging: false`. Une fenêtre partielle est vidée à la désactivation du
driver. Les sondes cumulent des compteurs sans construire de texte à chaque frame.

- `frame_wall_ms_avg`, `max`, `peak_frame`, `over33`, `over100` : durée réelle entre
  deux Update ; comprend aussi le travail du moteur, le rendu et les attentes.
- `update`, `navigation`, `phone_ui` : callbacks du driver, boucle du mod et panneau
  GPS ; cela ne couvre pas tout le moteur UI natif.
- `fixed`, `projection`, `sensor` : contrôle physique et ses sous-étapes.
- `plan`, `search`, `curve`, `traffic_bind`, `start` : préparation et confirmation.
- Pour chaque étape : durée moyenne et maximale en ms, nombre d'appels, frame du pic.
  Les étapes sont imbriquées : leurs durées ne doivent pas être additionnées.
- `boxes`, `overlaps`, `component_lookups`, `projection_segments`, `reused_plans` :
  volumes de travail dans la fenêtre.

Les événements `plan_reused`, `plan_refresh`, `drive_started` et `drive_stopped`
précisent la frame et le temps réel depuis le lancement. Un appui normal sur GO
après la confirmation doit produire `plan_reused` sans seconde requête de calcul.

La compilation et l'installation ne mesurent pas le gain d'images/s. Il faut un
nouvel essai en jeu pour comparer les saccades avec la session précédente.

## Révision 4 : vitesse autorisée et boîte du véhicule

- Suppression du plafond automatique fixe de 40 km/h et des plafonds issus de
  `Waypoint.maxSpeed`, qui décrivent l'allure désirée des PNJ. La cible suit la
  politique BetterFines : 50 km/h en ville, 80 dans les zones cartographiées,
  avec une marge cible de 0.36 km/h. Les routes élevées restent exclues du pilote ;
  la présence du CSV ne constitue pas une prise en charge des ponts.
- Le régulateur utilise toute la plage d'accélérateur et une correction intégrale
  bornée pour compenser la traînée. Il efface cette correction à l'arrêt, à la pause,
  à une baisse nette de cible ou au changement de politique. Les contraintes de
  courbure, freinage, feux, obstacles et arrivée restent prioritaires.
- Le profil conserve les contraintes de route/courbure séparément : un changement
  de plafond dans les options peut augmenter ou diminuer la vitesse sans recalculer
  le trajet. L'horizon des capteurs anticipe le freinage à 2 m/s², jusqu'à 150 m.
- Le pilote prend temporairement le délégué de décision des boîtes à plusieurs
  rapports, conserve leur type et appelle `TransmissionComponent.ShiftInto`.
  Hors engagement initial de première, les changements utilisent le délai natif,
  l'embrayage, la coupure moteur et les événements natifs de la boîte. Seuils de
  régime, projection du régime après changement et délai entre commandes limitent
  surrégime et oscillations. Les CVT conservent leur commande native.
- `gear_request`, `gear_engaged` et `drive_state` rendent les nouveaux comportements
  observables. `drive_state` est limité à une ligne toutes les cinq secondes :
  vitesses réelle/cible/route/profil/trafic, rapport, régime, accélérateur et frein.

Build complet réussi avec les DLL installées ; installation comparée fichier par
fichier. Aucune suite de tests ajoutée ou exécutée pour cette révision. Aucune
publication Steam.

## Révision 5 : piétons, dépassement et feux rouges

- Trois interrupteurs indépendants dans les options BAUI, sauvegardés par partie,
  synchronisés avec les PlayerPrefs et traduits dans les 22 langues. Valeurs par
  défaut : arrêt piétons activé, dépassement désactivé, feux rouges désactivés.
  Les modifications prennent effet pendant le trajet ; le prochain relevé de
  capteurs et de télémétrie est demandé immédiatement.
- Le mode automatique a un libellé distinct du respect des limitations. Le plafond
  manuel passe à 20–80 km/h. L'autorisation de dépassement agit sur le profil et le
  contrôle courant ; réactiver les limitations rétablit les contraintes anticipées
  conservées séparément des limites de courbure. Limiteur du véhicule, ralentissements
  en courbe, obstacles, véhicules et arrêt final restent pris en compte.
- Les personnages sont identifiés via `ThirdPersonCharacter`, conservé dans le
  cache de colliders. Désactiver l'arrêt piétons les exclut des balayages et des
  contrôles de trafic transversal, sans masquer les autres obstacles. Lorsque
  l'arrêt est activé, la liste vivante `pedestrianSpawner.activePedestrians` complète
  les balayages solides : position actuelle et position prédite jusqu'à trois
  secondes selon la vitesse NavMesh, projection locale sur le prochain tronçon.
  Cela couvre les piétons de rue dont les colliders sont des triggers ou désactivés.
- Les nœuds d'arrêt contrôlés par un feu sont identifiés dans
  `CurrentSceneData.allLightsIntersections[].stopWaypoints[].roadWaypoints` au
  rattachement du trafic. La dérogation ignore `stop` pour ces nœuds uniquement.
  `temporaryDisabled` et les autres nœuds d'arrêt restent bloquants. La vérification
  de trafic transversal reste active lors du franchissement d'un feu ; les règles
  des intersections de priorité conservent `CanChange`.
- `drive_state` inclut `stop_pedestrians`, `speeding`, `run_red`. Les amendes, les
  collisions, les feux et les piétons du jeu ne sont pas modifiés par ces options.

Compilation avec les DLL installées et installation des 33 fichiers réussies ;
config locale conservée. Aucune suite de tests ajoutée ou exécutée et aucun essai
interactif de ces nouveaux réglages effectué. Aucune publication Steam.
