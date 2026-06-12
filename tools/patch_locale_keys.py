#!/usr/bin/env python3
"""Deprecated — use generate_voogle_route_locales.py (single source of truth)."""

from __future__ import annotations

import json
from pathlib import Path

LOCALES_DIR = Path(__file__).resolve().parents[1] / "Locales"

EXTRA: dict[str, dict[str, str]] = {
    "en": {
        "voogle_route_options_indoor_route": "Indoor route line to exit",
        "voogle_route_options_indoor_autowalk": "Indoor auto-walk to exit",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "Set destination",
        "voogle_route_map_dest_confirm": "SET DESTINATION",
        "voogle_route_map_dest_cancel": "CANCEL",
    },
    "fr": {
        "voogle_route_options_indoor_route": "Ligne intérieure vers la sortie",
        "voogle_route_options_indoor_autowalk": "Marche auto intérieure vers la sortie",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "Définir la destination",
        "voogle_route_map_dest_confirm": "DÉFINIR LA DESTINATION",
        "voogle_route_map_dest_cancel": "ANNULER",
    },
    "de": {
        "voogle_route_options_indoor_route": "Innenroutenlinie zum Ausgang",
        "voogle_route_options_indoor_autowalk": "Automatisch im Gebäude zum Ausgang",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "Ziel setzen",
        "voogle_route_map_dest_confirm": "ZIEL SETZEN",
        "voogle_route_map_dest_cancel": "ABBRECHEN",
    },
    "es": {
        "voogle_route_options_indoor_route": "Línea interior hacia la salida",
        "voogle_route_options_indoor_autowalk": "Caminar automáticamente hacia la salida",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "Establecer destino",
        "voogle_route_map_dest_confirm": "ESTABLECER DESTINO",
        "voogle_route_map_dest_cancel": "CANCELAR",
    },
    "it": {
        "voogle_route_options_indoor_route": "Linea interna verso l'uscita",
        "voogle_route_options_indoor_autowalk": "Cammino automatico verso l'uscita",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "Imposta destinazione",
        "voogle_route_map_dest_confirm": "IMPOSTA DESTINAZIONE",
        "voogle_route_map_dest_cancel": "ANNULLA",
    },
    "pt-BR": {
        "voogle_route_options_indoor_route": "Linha interna até a saída",
        "voogle_route_options_indoor_autowalk": "Caminhar automaticamente até a saída",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "Definir destino",
        "voogle_route_map_dest_confirm": "DEFINIR DESTINO",
        "voogle_route_map_dest_cancel": "CANCELAR",
    },
    "ru": {
        "voogle_route_options_indoor_route": "Линия маршрута внутри к выходу",
        "voogle_route_options_indoor_autowalk": "Автоходьба внутри к выходу",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "Установить пункт назначения",
        "voogle_route_map_dest_confirm": "УСТАНОВИТЬ ПУНКТ НАЗНАЧЕНИЯ",
        "voogle_route_map_dest_cancel": "ОТМЕНА",
    },
    "pl": {
        "voogle_route_options_indoor_route": "Linia trasy wewnątrz do wyjścia",
        "voogle_route_options_indoor_autowalk": "Automatyczne przejście do wyjścia",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "Ustaw cel",
        "voogle_route_map_dest_confirm": "USTAW CEL",
        "voogle_route_map_dest_cancel": "ANULUJ",
    },
    "nl": {
        "voogle_route_options_indoor_route": "Binnenroutelijn naar uitgang",
        "voogle_route_options_indoor_autowalk": "Automatisch binnen naar uitgang",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "Bestemming instellen",
        "voogle_route_map_dest_confirm": "BESTEMMING INSTELLEN",
        "voogle_route_map_dest_cancel": "ANNULEREN",
    },
    "tr": {
        "voogle_route_options_indoor_route": "Çıkışa iç mekan rota çizgisi",
        "voogle_route_options_indoor_autowalk": "Çıkışa iç mekan otomatik yürüme",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "Hedef belirle",
        "voogle_route_map_dest_confirm": "HEDEF BELİRLE",
        "voogle_route_map_dest_cancel": "İPTAL",
    },
    "ja": {
        "voogle_route_options_indoor_route": "出口への屋内ルート線",
        "voogle_route_options_indoor_autowalk": "出口への屋内自動歩行",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "目的地を設定",
        "voogle_route_map_dest_confirm": "目的地を設定",
        "voogle_route_map_dest_cancel": "キャンセル",
    },
    "ko": {
        "voogle_route_options_indoor_route": "출구까지 실내 경로 선",
        "voogle_route_options_indoor_autowalk": "출구까지 실내 자동 걷기",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "목적지 설정",
        "voogle_route_map_dest_confirm": "목적지 설정",
        "voogle_route_map_dest_cancel": "취소",
    },
    "zh-CN": {
        "voogle_route_options_indoor_route": "室内至出口的路线",
        "voogle_route_options_indoor_autowalk": "室内自动步行至出口",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "设置目的地",
        "voogle_route_map_dest_confirm": "设置目的地",
        "voogle_route_map_dest_cancel": "取消",
    },
    "zh-TW": {
        "voogle_route_options_indoor_route": "室內至出口的路線",
        "voogle_route_options_indoor_autowalk": "室內自動步行至出口",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "設定目的地",
        "voogle_route_map_dest_confirm": "設定目的地",
        "voogle_route_map_dest_cancel": "取消",
    },
    "cs": {
        "voogle_route_options_indoor_route": "Vnitřní linie trasy k východu",
        "voogle_route_options_indoor_autowalk": "Automatická chůze k východu",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "Nastavit cíl",
        "voogle_route_map_dest_confirm": "NASTAVIT CÍL",
        "voogle_route_map_dest_cancel": "ZRUŠIT",
    },
    "da": {
        "voogle_route_options_indoor_route": "Indendørs rutelinje til udgang",
        "voogle_route_options_indoor_autowalk": "Automatisk gang til udgang",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "Angiv destination",
        "voogle_route_map_dest_confirm": "ANGIV DESTINATION",
        "voogle_route_map_dest_cancel": "ANNULLER",
    },
    "fi": {
        "voogle_route_options_indoor_route": "Sisäreittiviiva uloskäynnille",
        "voogle_route_options_indoor_autowalk": "Automaattinen kävely uloskäynnille",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "Aseta määränpää",
        "voogle_route_map_dest_confirm": "ASETA MÄÄRÄNPÄÄ",
        "voogle_route_map_dest_cancel": "PERUUTA",
    },
    "el": {
        "voogle_route_options_indoor_route": "Εσωτερική γραμμή προς την έξοδο",
        "voogle_route_options_indoor_autowalk": "Αυτόματη πορεία προς την έξοδο",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "Ορισμός προορισμού",
        "voogle_route_map_dest_confirm": "ΟΡΙΣΜΟΣ ΠΡΟΟΡΙΣΜΟΥ",
        "voogle_route_map_dest_cancel": "ΑΚΥΡΩΣΗ",
    },
    "hu": {
        "voogle_route_options_indoor_route": "Beltéri útvonalvonal a kijárathoz",
        "voogle_route_options_indoor_autowalk": "Automatikus séta a kijárathoz",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "Cél beállítása",
        "voogle_route_map_dest_confirm": "CÉL BEÁLLÍTÁSA",
        "voogle_route_map_dest_cancel": "MÉGSE",
    },
    "ro": {
        "voogle_route_options_indoor_route": "Linie de rută interioară spre ieșire",
        "voogle_route_options_indoor_autowalk": "Mers automat spre ieșire",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "Setează destinația",
        "voogle_route_map_dest_confirm": "SETEAZĂ DESTINAȚIA",
        "voogle_route_map_dest_cancel": "ANULEAZĂ",
    },
    "uk": {
        "voogle_route_options_indoor_route": "Лінія маршруту всередині до виходу",
        "voogle_route_options_indoor_autowalk": "Автохідьба всередині до виходу",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "Встановити пункт призначення",
        "voogle_route_map_dest_confirm": "ВСТАНОВИТИ ПУНКТ ПРИЗНАЧЕННЯ",
        "voogle_route_map_dest_cancel": "СКАСУВАТИ",
    },
    "lt": {
        "voogle_route_options_indoor_route": "Vidaus maršruto linija iki išėjimo",
        "voogle_route_options_indoor_autowalk": "Automatinis ėjimas iki išėjimo",
        "voogle_route_way_out_on": "WAY OUT",
        "voogle_route_way_out_off": "WAY OUT OFF",
        "voogle_route_get_out": "GET OUT",
        "voogle_route_get_out_on": "GET OUT ON",
        "voogle_route_map_dest_title": "Nustatyti tikslą",
        "voogle_route_map_dest_confirm": "NUSTATYTI TIKSLĄ",
        "voogle_route_map_dest_cancel": "ATŠAUKTI",
    },
}


def main() -> None:
    en_path = LOCALES_DIR / "en.json"
    with en_path.open(encoding="utf-8-sig") as f:
        en_data = json.load(f)
    key_order = list(en_data.keys())

    for path in sorted(LOCALES_DIR.glob("*.json")):
        locale = path.stem
        extras = EXTRA.get(locale, EXTRA["en"])

        with path.open(encoding="utf-8-sig") as f:
            data = json.load(f)

        for key in key_order:
            if key not in data:
                data[key] = extras.get(key, EXTRA["en"][key])

        ordered = {key: data[key] for key in key_order if key in data}
        with path.open("w", encoding="utf-8", newline="\n") as f:
            json.dump(ordered, f, ensure_ascii=False, indent=4)
            f.write("\n")
        print(f"patched {path.name}")


if __name__ == "__main__":
    main()
