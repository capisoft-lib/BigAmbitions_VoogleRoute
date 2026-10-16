# Conduite physique — Voogle Route 1.0.12

Base : VoogleRoute **1.0.11**, commit `45d97b0`, sur **Big Ambitions 1.0 Build 3682**
(Steam build 25482473, Unity 2022.3.62f2). Branche : `codex/physical-autodrive`.
Version publiée : `AssemblyInformationalVersion = 1.0.12`, reprenant la révision
de développement `1.0.11-physical-autodrive.7`. Preuves de publication :
[`releases/1.0.12/VALIDATION.md`](../releases/1.0.12/VALIDATION.md).

## Utilisation

1. Dans Échap → Options → Mods → Voogle Route, **Voir conduire par défaut** définit
   le mode présélectionné au départ. Désactiver ce réglage présélectionne le voyage
   rapide ; les deux modes restent proposés pour chaque trajet.
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
2. Monter dans une voiture, rejoindre une voie de circulation et s'arrêter dans
   son sens de circulation. Un départ depuis une place de parking n'est pas prévu.
3. Choisir une destination, demander AUTO-DRIVE, sélectionner **Voir conduire** ou
   **Voyage rapide**, vérifier les détails puis confirmer le départ. Le voyage
   rapide conserve l'estimation de durée, d'arrivée et de carburant. Si la conduite
   réelle ne trouve pas de trajet, sélectionner le voyage rapide dans la même fenêtre.
   Si la fenêtre est ouverte en roulant, Voir conduire devient disponible après
   l'arrêt et la préparation du trajet, sans fermer la fenêtre. Un départ exige
   toujours une voie praticable et une voiture alignée dans son sens.
4. Pendant Voir conduire, les touches de déplacement, l'interaction de sortie, Échap ou ARRÊTER rendent
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

Installation 1.0.12 du 16 octobre 2026 : 153 sources compilées, 33 fichiers livrés et comparés.
Empreinte SHA-256 du DLL installé :
`5914507B8840325FE8E9BA9C44E887E429C11DDF0389A6BF7096B34603266357`.
L'unique fichier utilisateur présent, `config.json`, conserve son empreinte
`59505653560F93D6E4A8273782B463001FB89049415019B37A877A04BE7EF07D`.

L'utilisateur a essayé la révision 6 ; ses logs montrent un trajet réel en AlfaGiulia
et il signale l'absence de rafraîchissement du dialogue après l'arrêt.
**Le rafraîchissement automatique de la révision 7 reste à observer en jeu.**
La compilation et les empreintes ne constituent
pas une mesure du comportement physique.
Il n'y a pas de marche arrière, de stationnement automatique ni de récupération par
téléportation. Les voies élevées de pont et souterraines sont exclues du graphe de
conduite de cette première version. Les trajets sans voie compatible sont refusés ; le voyage rapide reste
disponible au départ. La version 1.0.12 a été publiée sur l'élément Workshop existant
le 16 octobre 2026 ; les détails des anciennes révisions ci-dessous décrivent leurs
contrôles au moment de leur développement.

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

## Révision 6 : courbes, maintien de vitesse et choix au départ

### Constat de la session précédente

Dans Player.log du 16 octobre à 16:06, révision 5, le trajet en AlfaGiulia
montre `speed_kmh=13.3 target_kmh=14.4 path_kmh=14.4 traffic_kmh=80.0`
à `realtime_s=243.002` : le profil de courbure impose ici le ralentissement.
À `realtime_s=300.241`, `305.240`, `310.232`, `315.283`, les relevés donnent
respectivement 80.2, 80.4, 80.0 et 80.2 km/h pour une cible de 80 km/h.
Ils montrent un maintien proche de la cible sur cette portion, sans exclure des
variations plus courtes entre les relevés espacés de cinq secondes. D'autres
ralentissements sont explicitement imposés par le trafic ou le profil anticipé.

### Changements

- Courbure mesurée sur un voisinage de deux mètres de part et d'autre au lieu
  des seuls trois échantillons adjacents, pour réduire l'effet des petits raccords.
  Accélération latérale cible 3.5 m/s², contre 1.6 ; anticipation du freinage pour
  les courbes et l'arrivée à 3.5 m/s², contre 2. Les capteurs conservent leur
  anticipation plus prudente pour le trafic et les obstacles.
- Visée de suivi raccourcie selon la courbure pour ne pas couper un virage serré.
  Entre 1.5 et 3.5 m d'écart latéral, le plafond de récupération diminue
  progressivement de 43.2 à 10.8 km/h. Le freinage de sortie de voie est conservé.
- Correction intégrale signée (-0.25 à +0.7), permettant aussi de retirer un excès
  d'accélérateur à vitesse constante. Filtrage de l'accélération mesurée sur 0.2 s,
  correction de l'accélération, commande d'accélérateur progressive et absence
  d'apprentissage pendant la coupure de couple d'un changement de rapport.
  La correction est réinitialisée lors du ralentissement prévu et des pauses.
- La confirmation présente deux sélecteurs BAUI : Voir conduire et Voyage rapide,
  puis Annuler et Départ. Le corps affiche les détails propres au mode choisi.
  L'échec de préparation désactive le départ de ce mode en conservant l'autre
  choix accessible ; une préparation partielle en exception est rejetée.
- Le plan physique reste réutilisable à la confirmation avec ses contrôles de
  contexte et de pose. Le voyage rapide revérifie route et carburant au départ.
  La fenêtre est liée à la voiture, destination, sauvegarde et scène ; les anciens
  choix ne sont pas exécutés après changement de contexte.
- La clé sauvegardée `physicalAutoDrive` conserve sa compatibilité et définit
  maintenant la présélection (Voir conduire par défaut), sans interdire l'autre
  mode ni modifier un trajet en cours. Textes et choix disponibles dans 22 langues.
- Télémétrie enrichie : `lateral_m`, `cruise_kmh`, `shifting` pour distinguer plafond
  configuré, écart de suivi et coupure temporaire de transmission.

Build avec les DLL BA 1.0 installées réussi, quatre warnings TMP existants,
installation et empreintes comparées. Aucune suite de tests ajoutée ou exécutée.
Le réglage des courbes et la régulation restent à confirmer avec la physique réelle ;
le rendu BAUI et les deux départs n'ont pas été exercés dans le jeu pour cette révision.

## Révision 7 : disponibilité du dialogue et diagnostic des pauses

### Mesures sur la révision 6

Player.log de la session terminée à 16:37 le 16 octobre :

- Préparation du trajet : 216.509 ms, dont A* 54.085 ms, courbe 2.955 ms et
  rattachement au trafic 153.501 ms. Départ : 136.719 ms malgré un plan réutilisé.
  Ce sont des pauses mesurées dans des enveloppes VoogleRoute. Le détail ne permet
  pas encore de séparer initialisation à froid, travail métier et runtime dans ces phases.
- Pendant les fenêtres entièrement en conduite, contrôle physique moyen
  0.108–0.207 ms par tick, capteurs moyens 0.175–0.655 ms. Maximum régulier du
  contrôle 1.560 ms ; premier tick de démarrage 5.706 ms. Les 179–302 ms de certaines
  frames entières ne sont pas localisés par ces seules sondes du pilote.
- Des pics hors conduite existent aussi, dont navigation VoogleRoute 126.302 ms
  au chargement et 71.325 ms dans une fenêtre d'interaction avec la carte. Le coût
  du pilote en régime établi ne prouve pas l'absence de toutes les autres sources
  de saccades ; aucune régression globale entre sessions différentes n'est établie.
- Quatre refus `start_lane_not_found` à la même pose, puis un trajet valide après
  repositionnement : le rafraîchissement doit tenir compte du cap et de la position,
  en plus de l'arrêt. Un refus immobile reste valide et ne doit pas relancer A* en boucle.

### Correctif

- Surveillance de l'option Voir conduire toutes les 0.2 s, uniquement lorsque ce
  mode est sélectionné dans un dialogue ouvert. Lecture directe du Rigidbody
  conservé à l'ouverture ; aucune reconstruction de géométrie pour cette lecture.
- En mouvement au-dessus de 0.2 m/s, le plan est invalidé et le départ désactivé.
  Après au moins 0.25 s d'arrêt observé, une préparation est déclenchée et les détails
  ainsi que l'état du bouton sont actualisés. Avec la fréquence de lecture, prévoir
  0.4–0.6 s après l'arrêt, auxquels s'ajoute le calcul éventuel du trajet.
- Plan valide conservé à pose/options/contexte identiques. Un refus stationnaire
  n'est réessayé qu'après repositionnement d'au moins 0.5 m, rotation d'au moins
  5 degrés, changement de graphe/source de trafic ou paramètres de trajet.
  Un plan valide est renouvelé après déplacement de 1.5 m, rotation de 5 degrés,
  changement de paramètres/contexte ou expiration à 60 s. Un clic explicite peut
  réessayer un refus ; un plan valide reste réutilisé.
- Une nouvelle vérification légère est faite à la confirmation avant fermeture,
  afin de conserver le dialogue si la voiture est repartie entre deux lectures.
  Les contrôles complets de départ restent dans le service de conduite.
- Sondes supplémentaires : `preview`, `start_validate`, `fuel_check`, `acquire_input`,
  `traffic_index`, `traffic_match`. Elles sont imbriquées dans les étapes existantes
  et ne doivent pas être additionnées à leur parent. Leur ajout ne constitue pas
  une optimisation démontrée des pauses de préparation et de départ.

Compilation et installation locale réussies ; 33 fichiers comparés et config
utilisateur préservée. Aucun test ajouté/exécuté et aucun essai interactif de ce
correctif ; sa réactivité et son coût réel restent à mesurer lors du prochain essai.
