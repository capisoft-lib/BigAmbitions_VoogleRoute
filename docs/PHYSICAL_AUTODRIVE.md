# Conduite physique — branche de développement

Base : VoogleRoute **1.0.11**, commit `45d97b0`, sur **Big Ambitions 1.0 Build 3682**
(Steam build 25482473, Unity 2022.3.62f2). Branche : `codex/physical-autodrive`.
Le build local se distingue par `AssemblyInformationalVersion = 1.0.11-physical-autodrive`.

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
- `PhysicalRoutingGraph` filtre les demi-tours et courbes synthétiques incompatibles
  avec le rayon de braquage. Le départ appartient à une voie orientée comme le véhicule.
- `PhysicalDrivePlanner` utilise A* et le graphe routier existant. Il conserve les
  ancrages de voie, arrondit les angles praticables et vérifie la courbure. Aucune
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

Installation du 16 octobre 2026 : 149 sources compilées, 32 fichiers livrés et comparés.
Empreinte SHA-256 du DLL installé :
`1EACE24AD57B07F5B6E5AD59CC9D9AA0AEBD000DE3E7A2495891FDFB490C6545`.
L'unique fichier utilisateur présent, `config.json`, conserve son empreinte
`59505653560F93D6E4A8273782B463001FB89049415019B37A877A04BE7EF07D`.

**Aucun trajet ni rendu d'interface n'a encore été vérifié dans le jeu.** Les réglages
de suivi, feux, distances d'obstacle et freinage doivent être confirmés lors de l'essai.
Il n'y a pas de marche arrière, de stationnement automatique ni de récupération par
téléportation. Les voies élevées de pont et souterraines sont exclues du graphe de
conduite de cette première version. Les trajets sans voie compatible sont refusés ; le voyage rapide reste
disponible dans les options. Aucune publication Steam de cette branche n'est effectuée.
