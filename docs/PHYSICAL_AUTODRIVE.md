# Conduite physique — branche de développement

Base : VoogleRoute **1.0.11**, commit `45d97b0`, sur **Big Ambitions 1.0 Build 3682**
(Steam build 25482473, Unity 2022.3.62f2). Branche : `codex/physical-autodrive`.
Le build local courant se distingue par `AssemblyInformationalVersion = 1.0.11-physical-autodrive.2`.

## Utilisation

1. Dans Échap → Options → Mods → Voogle Route, laisser **Conduite réelle** activée.
   La vitesse maximale est réglable entre 20 et 60 km/h, avec 40 km/h par défaut.
   Ces deux réglages sont enregistrés dans les options de la sauvegarde.
   Désactiver Conduite réelle rétablit le voyage rapide existant.
2. Monter dans une voiture, rejoindre une voie de circulation et s'arrêter dans
   son sens de circulation. Un départ depuis une place de parking n'est pas prévu.
3. Choisir une destination, demander AUTO-DRIVE puis confirmer.
4. Les touches de déplacement, l'interaction de sortie, Échap ou ARRÊTER rendent
   immédiatement la main. À l'arrivée sur la voie, stationner manuellement.

## Implémentation

- `VehicleControlLease` lit l'empattement des vraies roues NWH et prend seulement
  les entrées de la voiture occupée. Les réglages `autoSetInput`,
  `swapInputInReverse`, `useRawInput` et l'assistance externe sont restaurés.
- `PhysicalRoutingGraph` filtre les demi-tours et transitions interdites, et préfère
  les raccords larges. Le départ appartient à une voie orientée comme le véhicule.
- `PhysicalDrivePlanner` utilise A* et le graphe routier existant. Il conserve les
  ancrages de voie, lisse les courbes complètes et vérifie la courbure. Aucune
  corde finale vers le bâtiment n'est ajoutée. Une route incompatible est refusée.
- `DrivingPath` suit une progression locale monotone, avec visée depuis l'essieu
  arrière et profil de vitesse anticipant virages, limites de voie et arrêt final.
- `DrivingTrafficSensor` relie les positions du graphe aux waypoints Gley **vivants**,
  puis lit `stop`, `giveWay`, `CanChange` et les limites de vitesse. Des balayages de
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
symboles de débogage, construit les 32 fichiers du paquet dans `Output/VoogleRoute`
puis les recopie dans `ModsLocal/VoogleRoute`. Il refuse une installation pendant
que le jeu tourne, préserve les fichiers utilisateur et compare les empreintes de
tous les fichiers livrés. Les seules dépendances sont celles déjà requises par le
mod ; la bibliothèque Pathfinding reste inchangée.

## Preuves et limites

Compilation du code complet avec les DLL du jeu installé : réussie. Profil Player
contrôlé. Quatre avertissements préexistants concernent `TMP_Text.enableWordWrapping`.
Les fichiers du paquet et les fichiers utilisateur sont contrôlés lors de l'installation.

Installation révision 2 du 16 octobre 2026 : 151 sources compilées, 32 fichiers livrés et comparés.
Empreinte SHA-256 du DLL installé :
`B9205F090231BB03EC4962CC02F5E61D14F020E555B454A00DE3702111CD60EC`.
L'unique fichier utilisateur présent, `config.json`, conserve son empreinte
`59505653560F93D6E4A8273782B463001FB89049415019B37A877A04BE7EF07D`.

**Aucun trajet ni rendu d'interface n'a encore été vérifié dans le jeu.** Les réglages
de suivi, feux, distances d'obstacle et freinage doivent être confirmés lors de l'essai.
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
