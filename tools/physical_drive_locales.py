"""Physical driving strings; merge into existing files without replacing other UI keys."""
import json
from pathlib import Path

KEYS = [
    "voogle_route_options_physical_autodrive", "voogle_route_options_physical_drive_speed",
    "voogle_route_physical_stop", "voogle_route_physical_body", "voogle_route_physical_no_route",
    "voogle_route_physical_start_lane", "voogle_route_physical_blocked", "voogle_route_physical_arrived",
]
TEXT = {
    "en": ["Auto-drive: real driving (beta; off = fast travel)", "Maximum driving speed", "STOP DRIVING",
           "Drive your car along the streets: ~{distance} m.\nMovement keys or STOP DRIVING: take over.\nArrival on the road; park manually.",
           "No drivable route for this car. Try another destination or use fast travel in the mod options.",
           "Stop on a traffic lane, facing its direction, before starting auto-drive.",
           "Auto-drive interrupted: blocked road, lane departure or unavailable vehicle. Take the wheel.",
           "Destination reached. Take the wheel to park."],
    "fr": ["Conduite réelle (bêta ; désactivé = voyage rapide)", "Vitesse maximale de conduite", "ARRÊTER",
           "Votre voiture suivra les rues sur ~{distance} m.\nTouches de déplacement ou ARRÊTER : reprendre le volant.\nArrivée sur la route ; stationnement manuel.",
           "Aucun trajet praticable pour cette voiture. Changez de destination ou activez le voyage rapide dans les options du mod.",
           "Arrêtez-vous sur une voie de circulation, dans le sens de la voie, avant de lancer la conduite automatique.",
           "Conduite interrompue : obstacle prolongé, sortie de voie ou véhicule indisponible. Reprenez le volant.",
           "Destination atteinte. Reprenez le volant pour stationner."],
    "de": ["Echtes Fahren (Beta; aus = Schnellreise)", "Maximale Fahrgeschwindigkeit", "ANHALTEN",
           "Ihr Auto folgt den Straßen: ~{distance} m.\nBewegungstasten oder ANHALTEN: übernehmen.\nAnkunft auf der Straße; selbst parken.",
           "Keine befahrbare Route für dieses Auto. Anderes Ziel oder Schnellreise wählen.",
           "Vor dem Start auf einer Fahrspur in Fahrtrichtung anhalten.", "Autofahrt unterbrochen. Bitte das Steuer übernehmen.", "Ziel erreicht. Bitte selbst parken."],
    "es": ["Conducción real (beta; no = viaje rápido)", "Velocidad máxima", "DETENER",
           "Tu coche seguirá las calles: ~{distance} m.\nTeclas de movimiento o DETENER: retomar el control.\nLlegada en la calle; aparca manualmente.",
           "No hay ruta transitable para este coche. Elige otro destino o viaje rápido.", "Detente en un carril en su sentido antes de iniciar.",
           "Conducción interrumpida. Retoma el control.", "Destino alcanzado. Aparca manualmente."],
    "it": ["Guida reale (beta; no = viaggio rapido)", "Velocità massima", "FERMA",
           "La tua auto seguirà le strade: ~{distance} m.\nTasti di movimento o FERMA: riprendi il controllo.\nArrivo in strada; parcheggio manuale.",
           "Nessun percorso praticabile. Cambia destinazione o usa il viaggio rapido.", "Fermati su una corsia nel senso di marcia prima di iniziare.",
           "Guida interrotta. Riprendi il volante.", "Destinazione raggiunta. Parcheggia manualmente."],
    "pt-BR": ["Direção real (beta; não = viagem rápida)", "Velocidade máxima", "PARAR",
           "Seu carro seguirá as ruas: ~{distance} m.\nTeclas de movimento ou PARAR: assumir o controle.\nChegada na rua; estacione manualmente.",
           "Nenhuma rota transitável. Mude o destino ou use viagem rápida.", "Pare numa faixa no sentido do trânsito antes de iniciar.",
           "Direção interrompida. Assuma o volante.", "Destino alcançado. Estacione manualmente."],
    "nl": ["Echt rijden (bèta; uit = snelreizen)", "Maximumsnelheid", "STOPPEN",
           "Je auto volgt de straten: ~{distance} m.\nBewegingstoetsen of STOPPEN: neem over.\nAankomst op de weg; parkeer zelf.",
           "Geen berijdbare route. Kies een ander doel of snelreizen.", "Stop op een rijstrook in de rijrichting om te beginnen.",
           "Rit onderbroken. Neem het stuur over.", "Bestemming bereikt. Parkeer zelf."],
    "da": ["Rigtig kørsel (beta; fra = hurtig rejse)", "Maksimal hastighed", "STOP",
           "Din bil følger gaderne: ~{distance} m.\nBevægelsestaster eller STOP: overtag.\nAnkomst på vejen; parkér selv.",
           "Ingen farbar rute. Vælg et andet mål eller hurtig rejse.", "Stands i en kørebane i kørselsretningen før start.",
           "Kørsel afbrudt. Overtag rattet.", "Målet er nået. Parkér selv."],
    "fi": ["Oikea ajaminen (beta; pois = pikamatka)", "Enimmäisnopeus", "PYSÄYTÄ",
           "Autosi seuraa katuja: ~{distance} m.\nLiikenäppäimet tai PYSÄYTÄ: ota ohjat.\nSaapuminen tielle; pysäköi itse.",
           "Ei ajettavaa reittiä. Valitse toinen kohde tai pikamatka.", "Pysähdy ajokaistalle sen kulkusuuntaan ennen aloitusta.",
           "Ajo keskeytyi. Ota ohjat.", "Kohde saavutettu. Pysäköi itse."],
    "cs": ["Skutečná jízda (beta; vypnuto = rychlé cestování)", "Maximální rychlost", "ZASTAVIT",
           "Auto pojede ulicemi: ~{distance} m.\nPohybové klávesy nebo ZASTAVIT: převzít řízení.\nPříjezd na silnici; zaparkujte ručně.",
           "Žádná sjízdná trasa. Změňte cíl nebo použijte rychlé cestování.", "Před spuštěním zastavte v jízdním pruhu ve směru jízdy.",
           "Jízda přerušena. Převezměte řízení.", "Cíl dosažen. Zaparkujte ručně."],
    "pl": ["Rzeczywista jazda (beta; wył. = szybka podróż)", "Maksymalna prędkość", "ZATRZYMAJ",
           "Samochód pojedzie ulicami: ~{distance} m.\nKlawisze ruchu lub ZATRZYMAJ: przejmij sterowanie.\nPrzyjazd na drogę; zaparkuj ręcznie.",
           "Brak przejezdnej trasy. Zmień cel lub wybierz szybką podróż.", "Przed startem zatrzymaj się na pasie zgodnie z kierunkiem jazdy.",
           "Jazda przerwana. Przejmij kierownicę.", "Cel osiągnięty. Zaparkuj ręcznie."],
    "hu": ["Valódi vezetés (béta; ki = gyorsutazás)", "Legnagyobb sebesség", "LEÁLLÍTÁS",
           "Az autó az utcákat követi: ~{distance} m.\nMozgásgombok vagy LEÁLLÍTÁS: átvétel.\nÉrkezés az úton; parkolj kézzel.",
           "Nincs járható útvonal. Válassz másik célt vagy gyorsutazást.", "Indítás előtt állj meg egy sávban a menetirány felé.",
           "Vezetés megszakítva. Vedd át az irányítást.", "Cél elérve. Parkolj kézzel."],
    "ro": ["Condus real (beta; oprit = călătorie rapidă)", "Viteză maximă", "OPREȘTE",
           "Mașina va urma străzile: ~{distance} m.\nTastele de mișcare sau OPREȘTE: preia controlul.\nSosire pe șosea; parcare manuală.",
           "Nicio rută practicabilă. Schimbă destinația sau alege călătoria rapidă.", "Oprește pe o bandă în sensul de mers înainte de pornire.",
           "Condus întrerupt. Preia volanul.", "Destinație atinsă. Parchează manual."],
    "tr": ["Gerçek sürüş (beta; kapalı = hızlı seyahat)", "Azami hız", "DURDUR",
           "Aracınız sokakları izleyecek: ~{distance} m.\nHareket tuşları veya DURDUR: kontrolü alın.\nYolda varış; elle park edin.",
           "Sürülebilir rota yok. Hedefi değiştirin veya hızlı seyahati seçin.", "Başlamadan önce bir şeritte sürüş yönünde durun.",
           "Sürüş kesildi. Direksiyonu alın.", "Hedefe ulaşıldı. Elle park edin."],
    "el": ["Πραγματική οδήγηση (beta· εκτός = γρήγορο ταξίδι)", "Μέγιστη ταχύτητα", "ΔΙΑΚΟΠΗ",
           "Το αυτοκίνητο θα ακολουθήσει τους δρόμους: ~{distance} m.\nΠλήκτρα κίνησης ή ΔΙΑΚΟΠΗ: ανάληψη ελέγχου.\nΆφιξη στον δρόμο· χειροκίνητη στάθμευση.",
           "Δεν υπάρχει βατή διαδρομή. Αλλάξτε προορισμό ή επιλέξτε γρήγορο ταξίδι.", "Σταματήστε σε λωρίδα προς τη φορά κίνησης πριν ξεκινήσετε.",
           "Η οδήγηση διακόπηκε. Αναλάβετε το τιμόνι.", "Φτάσατε. Σταθμεύστε χειροκίνητα."],
    "lt": ["Tikras vairavimas (beta; išjungta = greita kelionė)", "Didžiausias greitis", "SUSTABDYTI",
           "Automobilis važiuos gatvėmis: ~{distance} m.\nJudėjimo klavišai arba SUSTABDYTI: perimti valdymą.\nAtvykimas kelyje; pastatykite rankiniu būdu.",
           "Nėra pravažiuojamo maršruto. Pakeiskite tikslą arba rinkitės greitą kelionę.", "Prieš pradėdami sustokite eismo juostoje važiavimo kryptimi.",
           "Važiavimas nutrauktas. Perimkite vairą.", "Tikslas pasiektas. Pastatykite automobilį."],
    "ru": ["Реальное вождение (бета; выкл. = быстрое перемещение)", "Максимальная скорость", "ОСТАНОВИТЬ",
           "Машина поедет по улицам: ~{distance} м.\nКлавиши движения или ОСТАНОВИТЬ: взять управление.\nПрибытие на дороге; парковка вручную.",
           "Нет проезжаемого маршрута. Измените цель или выберите быстрое перемещение.", "Перед началом остановитесь на полосе по направлению движения.",
           "Вождение прервано. Возьмите управление.", "Цель достигнута. Припаркуйтесь вручную."],
    "uk": ["Справжнє водіння (бета; вимк. = швидка подорож)", "Максимальна швидкість", "ЗУПИНИТИ",
           "Авто поїде вулицями: ~{distance} м.\nКлавіші руху або ЗУПИНИТИ: взяти керування.\nПрибуття на дорозі; паркування вручну.",
           "Немає проїзного маршруту. Змініть ціль або оберіть швидку подорож.", "Перед початком зупиніться на смузі за напрямком руху.",
           "Водіння перервано. Візьміть керування.", "Мети досягнуто. Припаркуйтеся вручну."],
    "ja": ["実際に運転（ベータ・オフ＝高速移動）", "最高速度", "運転を停止",
           "自分の車で道路を走行：約{distance} m。\n移動キーまたは「運転を停止」で操作を再開。\n道路上に到着します。駐車は手動です。",
           "この車で走行可能な経路がありません。目的地を変更するか高速移動を選択してください。", "開始前に車線上で進行方向に向けて停止してください。",
           "自動運転を中断しました。運転を引き継いでください。", "目的地に到着しました。手動で駐車してください。"],
    "ko": ["실제 주행 (베타; 끄면 빠른 이동)", "최대 주행 속도", "주행 중지",
           "자동차로 도로를 따라 약 {distance} m 주행합니다.\n이동 키 또는 주행 중지: 직접 조작.\n도로에 도착하며 주차는 수동입니다.",
           "이 차로 주행 가능한 경로가 없습니다. 목적지를 바꾸거나 빠른 이동을 선택하세요.", "시작 전에 차선에서 주행 방향을 향해 정차하세요.",
           "자동 주행이 중단되었습니다. 직접 운전하세요.", "목적지에 도착했습니다. 직접 주차하세요."],
    "zh-CN": ["实际驾驶（测试版；关闭＝快速旅行）", "最高驾驶速度", "停止驾驶",
           "驾驶自己的汽车沿街道行驶：约 {distance} 米。\n按移动键或“停止驾驶”接管。\n到达道路上，需手动停车。",
           "此车没有可行驶路线。请更改目的地或选择快速旅行。", "开始前请在车道内沿行驶方向停车。",
           "自动驾驶已中断，请接管方向盘。", "已到达目的地，请手动停车。"],
    "zh-TW": ["實際駕駛（測試版；關閉＝快速旅行）", "最高駕駛速度", "停止駕駛",
           "駕駛自己的汽車沿街道行駛：約 {distance} 公尺。\n按移動鍵或「停止駕駛」接管。\n抵達道路上，需手動停車。",
           "此車沒有可行駛路線。請更改目的地或選擇快速旅行。", "開始前請在車道內沿行駛方向停車。",
           "自動駕駛已中斷，請接管方向盤。", "已抵達目的地，請手動停車。"],
}


TURN_TIGHT = {
    "en": "A road route exists, but its turns cannot be fitted to this car. Choose another destination or fast travel.",
    "fr": "Une route existe, mais ses virages restent trop serrés pour cette voiture. Changez de destination ou utilisez le voyage rapide.",
    "de": "Eine Straßenroute existiert, aber ihre Kurven sind zu eng für dieses Auto. Anderes Ziel oder Schnellreise wählen.",
    "es": "Hay una ruta, pero sus curvas son demasiado cerradas para este coche. Cambia de destino o usa viaje rápido.",
    "it": "Una strada esiste, ma le curve sono troppo strette per questa auto. Cambia destinazione o usa il viaggio rapido.",
    "pt-BR": "Existe uma rota, mas as curvas são estreitas demais para este carro. Mude o destino ou use viagem rápida.",
    "nl": "Er is een route, maar de bochten zijn te krap voor deze auto. Kies een ander doel of snelreizen.",
    "da": "Der findes en rute, men svingene er for snævre til denne bil. Vælg et andet mål eller hurtig rejse.",
    "fi": "Reitti on olemassa, mutta sen mutkat ovat liian tiukkoja tälle autolle. Vaihda kohdetta tai käytä pikamatkaa.",
    "cs": "Trasa existuje, ale zatáčky jsou pro toto auto příliš těsné. Změňte cíl nebo použijte rychlé cestování.",
    "pl": "Trasa istnieje, ale zakręty są zbyt ciasne dla tego auta. Zmień cel lub użyj szybkiej podróży.",
    "hu": "Van útvonal, de a kanyarok túl szűkek ehhez az autóhoz. Válassz másik célt vagy gyorsutazást.",
    "ro": "Există o rută, dar virajele sunt prea strânse pentru această mașină. Schimbă destinația sau folosește călătoria rapidă.",
    "tr": "Bir rota var, ancak dönüşler bu araç için çok dar. Hedefi değiştirin veya hızlı seyahati kullanın.",
    "el": "Υπάρχει διαδρομή, αλλά οι στροφές είναι πολύ κλειστές για αυτό το όχημα. Αλλάξτε προορισμό ή επιλέξτε γρήγορο ταξίδι.",
    "lt": "Maršrutas yra, tačiau posūkiai šiam automobiliui per siauri. Pakeiskite tikslą arba rinkitės greitą kelionę.",
    "ru": "Маршрут есть, но повороты слишком крутые для этой машины. Измените цель или выберите быстрое перемещение.",
    "uk": "Маршрут є, але повороти надто круті для цього авто. Змініть ціль або оберіть швидку подорож.",
    "ja": "経路はありますが、この車にはカーブが急すぎます。目的地を変更するか高速移動を利用してください。",
    "ko": "경로는 있지만 이 차로 돌기에는 커브가 너무 급합니다. 목적지를 바꾸거나 빠른 이동을 이용하세요.",
    "zh-CN": "道路路线存在，但转弯对这辆车仍然太急。请更改目的地或使用快速旅行。",
    "zh-TW": "道路路線存在，但轉彎對這輛車仍然太急。請更改目的地或使用快速旅行。",
}


SPEED_OPTIONS = {
    "en": ("Follow road speed limits", "Speed cap (automatic mode off)"),
    "fr": ("Rouler à la limite de la route", "Vitesse maxi (mode automatique désactivé)"),
    "de": ("Tempolimit der Straße einhalten", "Höchsttempo (Automatikmodus aus)"),
    "es": ("Seguir el límite de la vía", "Velocidad máxima (modo automático desactivado)"),
    "it": ("Seguire i limiti stradali", "Velocità massima (modalità automatica disattivata)"),
    "pt-BR": ("Seguir o limite da via", "Velocidade máxima (modo automático desligado)"),
    "nl": ("Snelheidslimiet van de weg volgen", "Snelheidsgrens (automatische modus uit)"),
    "da": ("Følg vejens hastighedsgrænse", "Maksimal fart (automatisk tilstand slået fra)"),
    "fi": ("Noudata tien nopeusrajoitusta", "Enimmäisnopeus (automaattitila pois)"),
    "cs": ("Dodržovat rychlostní limit silnice", "Maximální rychlost (automatický režim vypnutý)"),
    "pl": ("Przestrzegaj limitu prędkości drogi", "Maksymalna prędkość (tryb automatyczny wyłączony)"),
    "hu": ("Közúti sebességhatár követése", "Sebességkorlát (automatikus mód kikapcsolva)"),
    "ro": ("Respectă limita de viteză a drumului", "Viteză maximă (mod automat dezactivat)"),
    "tr": ("Yolun hız sınırına uy", "Azami hız (otomatik mod kapalı)"),
    "el": ("Τήρηση ορίου ταχύτητας δρόμου", "Μέγιστη ταχύτητα (αυτόματη λειτουργία εκτός)"),
    "lt": ("Laikytis kelio greičio ribojimo", "Didžiausias greitis (automatinis režimas išjungtas)"),
    "ru": ("Соблюдать ограничение скорости дороги", "Предел скорости (авторежим выключен)"),
    "uk": ("Дотримуватися обмеження швидкості дороги", "Максимальна швидкість (авторежим вимкнено)"),
    "ja": ("道路の制限速度に合わせる", "上限速度（自動モードがオフのとき）"),
    "ko": ("도로 제한 속도 따르기", "최대 속도 (자동 모드 꺼짐)"),
    "zh-CN": ("按道路限速行驶", "最高速度（自动模式关闭时）"),
    "zh-TW": ("依道路速限行駛", "最高速度（自動模式關閉時）"),
}


def strings(locale):
    result = dict(zip(KEYS, TEXT[locale]))
    result["voogle_route_options_physical_drive_speed_value"] = "{value} km/h"
    result["voogle_route_physical_turn_tight"] = TURN_TIGHT[locale]
    result["voogle_route_options_physical_speed_auto"], result["voogle_route_options_physical_drive_speed"] = SPEED_OPTIONS[locale]
    return result


def main():
    root = Path(__file__).resolve().parents[1] / "Locales"
    for path in root.glob("*.json"):
        data = json.loads(path.read_text(encoding="utf-8-sig"))
        data.update(strings(path.stem))
        path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
