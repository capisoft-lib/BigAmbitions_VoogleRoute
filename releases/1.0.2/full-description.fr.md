[b]Voogle Route 1.0.2[/b]

Voogle Route enrichit Voogle Maps avec une ligne d’itinéraire claire au sol, la navigation à pied, des trajets routiers adaptés aux véhicules, le recours au métro, des favoris, un historique des visites et un voyage rapide facultatif. Le même paquet Voogle Route 1.0.2 prend en charge Big Ambitions EA 0.11 et la version 1.0.

Le graphe routier de la version 1.0 est inclus pour les deux versions du jeu. Sur EA 0.11, Voogle Route l’utilise pour la ville d’origine, tandis que les fonctions propres aux Hamptons restent automatiquement désactivées.

La version 1.0.2 privilégie la stabilité et les performances du routage. Les trajets véhicule impossibles ne sont plus recalculés indéfiniment, la recherche routière réutilise une mémoire bornée et les destinations à sens unique d’Industry City peuvent choisir une route d’arrivée proche réellement atteignable.

[b]Éléments du Workshop requis[/b]

[list]
[*][b]LIB BA Player Location 1.0.0 ou version ultérieure[/b]
[*][b]LIB BA Unified UI 1.0.0 ou version ultérieure[/b]
[/list]

Abonnez-vous aux deux bibliothèques et activez-les avant d’activer Voogle Route. Voogle Route n’intègre plus sa propre copie de la bibliothèque d’interface.

[b]Nouveautés de la version 1.0.2[/b]

[list]
[*][b]Fin des boucles d’échec[/b] — un même trajet véhicule ou intérieur est retenté avec temporisation, puis bloqué après trois échecs jusqu’à un changement réel de destination, de mode ou de position
[*][b]Routage borné plus rapide[/b] — une recherche multi-départs/multi-arrivées annulable réutilise ses tableaux et son tas binaire au lieu de lancer de nombreux A* fortement allocateurs
[*][b]Arrivées à sens unique atteignables[/b] — un repli borné et diversifié par direction choisit une route proche réellement accessible lorsque les voies les plus proches forment une poche orientée fermée
[*][b]Correctifs Industry City[/b] — restaure le demi-tour terminal audité de la Road 213 et empêche les voies denses de la Road 236 de masquer une arrivée atteignable voisine
[*][b]Résultats asynchrones sûrs[/b] — une recherche véhicule annulée ou remplacée ne peut plus écraser la demande active
[*][b]Diagnostics tamponnés[/b] — écritures regroupées, échecs et recalculs ignorés limités, et messages coûteux non construits lorsque la journalisation est désactivée
[*][b]Trajets à pied sensibles au mouvement[/b] — recalcul moins fréquent et seulement après un déplacement utile ; les agents NavMesh inactifs ou détachés sont rejetés avant le calcul
[/list]

[b]Fonctions de navigation et d’interface incluses[/b]

[list]
[*][b]Trois sélecteurs de couleur[/b] — modifiez les trajets à pied, en intérieur et en véhicule dans Options → Mods ; le tracé actif change immédiatement et la couleur est enregistrée par sauvegarde
[*][b]Listes défilantes[/b] — les Favoris et l’Historique affichent une barre de défilement visible et déplaçable lorsque les lignes débordent
[*][b]Favoris correctement séparés[/b] — les visites de l’Historique n’apparaissent pas comme favoris ; les favoris utilisateur, raccourcis et voitures sont conservés
[*][b]Distances actualisées[/b] — les valeurs visibles suivent les déplacements extérieurs, utilisent l’entrée du bâtiment en intérieur sans recalcul inutile et affichent correctement zéro à destination
[*][b]Tracé restauré après la carte[/b] — fermer M réaffiche immédiatement la ligne active au sol, à pied comme en voiture
[*][b]Un seul paquet pour EA 0.11 et la version 1.0[/b] — les adaptateurs choisissent automatiquement les API adaptées au lancement ; il n’existe pas de version héritée distincte
[*][b]Routes de Big Ambitions 1.0[/b] — données routières actualisées et itinéraires bidirectionnels couvrant toutes les adresses des manoirs des Hamptons
[*][b]Navigation dans les Hamptons[/b] — approches dédiées pour les manoirs et calcul privilégiant les entrées pour SORTIE / SORTIR aux portails
[*][b]Lignes d’itinéraire stables[/b] — le rendu hybride conserve des tracés fins et visibles à pied, en véhicule, en intérieur, dans le métro et sur la carte de la ville
[*][b]Fenêtres déplaçables[/b] — les panneaux interactifs peuvent être déplacés et mémorisent leur position
[*][b]Raccourcis configurables[/b] — les actions d’itinéraire et de déplacement automatique peuvent être réassignées dans Options → Mods ; Ctrl+Maj+Y et Ctrl+Maj+X par défaut
[*][b]Masqué sur l’ordinateur[/b] — le panneau et l’Historique ouvert se masquent pendant les jeux sur ordinateur puis retrouvent leur affichage précédent
[*][b]Protection à l’arrivée en taxi[/b] — empêche une entrée immédiate dans un bâtiment après une téléportation en taxi du jeu de base
[/list]

[b]Fonctionnalités principales[/b]

[list]
[*]Lignes d’itinéraire au sol pour les déplacements à pied, la navigation intérieure et les véhicules motorisés
[*]Marche auto avec itinéraire facultatif à pied → métro → à pied
[*]Guidage routier pour les véhicules et voyage rapide facultatif avec CONDUITE AUTO
[*]Tracé sur la carte de la ville, actions Y ALLER (AUTO) / Y ALLER (À PIED) et favoris avec recherche
[*]Historique des visites avec distances, définition de destination, centrage et ajout aux favoris
[*]Navigation rapide vers la dernière voiture, le domicile, le magasin et les véhicules stationnés qui vous appartiennent
[*]Couleurs d’itinéraire personnalisables et interface disponible dans 22 langues
[/list]

[b]Important : CONDUITE AUTO[/b]

CONDUITE AUTO est un voyage rapide accompagné d’une avance du temps. Cette fonction ne conduit pas physiquement le véhicule le long de la ligne. Avant confirmation, l’écran indique la distance, la durée estimée, l’heure d’arrivée et la consommation de carburant.

[b]Installation / mise à jour[/b]

[list]
[*]Abonnez-vous à LIB BA Player Location 1.0.0+ et activez-la
[*]Abonnez-vous à LIB BA Unified UI 1.0.0+ et activez-la
[*]Abonnez-vous à Voogle Route 1.0.2 et activez-le
[*]Redémarrez Big Ambitions une fois la mise à jour des trois éléments du Workshop terminée
[/list]

[b]Soutenir le développeur ☕[/b]

Si Voogle Route vous a évité de vous perdre — ou d’oublier où vous avez garé votre voiture — vous pouvez soutenir son développement en m’offrant un café :

[url=https://buymeacoffee.com/capitaine]☕ M’offrir un café[/url]

Le café garde le développeur éveillé, pour que la marche auto puisse continuer à marcher à sa place.
