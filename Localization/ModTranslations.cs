using System.Collections.Generic;

namespace VoogleRoute.Localization;

internal static class ModTranslations
{
    private static readonly Dictionary<string, Dictionary<StringKey, string>> ByLocale = new(StringComparer.OrdinalIgnoreCase);

    static ModTranslations()
    {
        Register("en",
            "VOOGLE ROUTE", "ROUTE ON", "ROUTE OFF", "AUTO WALK", "WALK ON", "{0} m",
            "Continue straight to destination", "Arriving", "Destination nearby", "Follow the route",
            "Continue straight", "Bear left", "Turn left", "Turn sharp left", "Bear right", "Turn right",
            "Turn sharp right", "Make a U-turn", "Destination", "Follow the route");
        Register("fr",
            "VOOGLE ROUTE", "ROUTE ON", "ROUTE OFF", "MARCHE AUTO", "MARCHE ON", "{0} m",
            "Continuez tout droit vers la destination", "Arrivée", "Destination proche", "Suivez la route",
            "Continuez tout droit", "Légèrement à gauche", "Tournez à gauche", "Tournez fortement à gauche",
            "Légèrement à droite", "Tournez à droite", "Tournez fortement à droite", "Faites demi-tour",
            "Destination", "Suivez la route");
        Register("de",
            "VOOGLE ROUTE", "ROUTE AN", "ROUTE AUS", "AUTO GEHEN", "GEHEN AN", "{0} m",
            "Geradeaus zur Destination", "Ankunft", "Ziel in der Nähe", "Route folgen",
            "Geradeaus weiter", "Leicht links", "Links abbiegen", "Scharf links",
            "Leicht rechts", "Rechts abbiegen", "Scharf rechts", "Wenden", "Ziel", "Route folgen");
        Register("es",
            "VOOGLE ROUTE", "RUTA ON", "RUTA OFF", "AUTO CAMINAR", "CAMINAR ON", "{0} m",
            "Siga recto hacia el destino", "Llegada", "Destino cercano", "Siga la ruta",
            "Siga recto", "Ligera a la izquierda", "Gire a la izquierda", "Gire bruscamente a la izquierda",
            "Ligera a la derecha", "Gire a la derecha", "Gire bruscamente a la derecha", "Haga un cambio de sentido",
            "Destino", "Siga la ruta");
        Register("it",
            "VOOGLE ROUTE", "ROUTE ON", "ROUTE OFF", "AUTO CAMMINO", "CAMMINO ON", "{0} m",
            "Prosegui dritto verso la destinazione", "Arrivo", "Destinazione vicina", "Segui il percorso",
            "Prosegui dritto", "Leggermente a sinistra", "Svolta a sinistra", "Svolta netta a sinistra",
            "Leggermente a destra", "Svolta a destra", "Svolta netta a destra", "Inversione a U",
            "Destinazione", "Segui il percorso");
        Register("pt-BR",
            "VOOGLE ROUTE", "ROTA ON", "ROTA OFF", "AUTO ANDAR", "ANDAR ON", "{0} m",
            "Siga em frente até o destino", "Chegada", "Destino próximo", "Siga a rota",
            "Siga em frente", "Leve à esquerda", "Vire à esquerda", "Vire acentuadamente à esquerda",
            "Leve à direita", "Vire à direita", "Vire acentuadamente à direita", "Faça retorno",
            "Destino", "Siga a rota");
        Register("ru",
            "VOOGLE ROUTE", "МАРШРУТ ВКЛ", "МАРШРУТ ВЫКЛ", "АВТО ШАГ", "ШАГ ВКЛ", "{0} м",
            "Продолжайте прямо к пункту назначения", "Прибытие", "Пункт рядом", "Следуйте по маршруту",
            "Продолжайте прямо", "Слегка налево", "Поверните налево", "Резко налево",
            "Слегка направо", "Поверните направо", "Резко направо", "Развернитесь", "Пункт назначения",
            "Следуйте по маршруту");
        Register("pl",
            "VOOGLE ROUTE", "TRASA ON", "TRASA OFF", "AUTO CHÓD", "CHÓD ON", "{0} m",
            "Jedź prosto do celu", "Przyjazd", "Cel w pobliżu", "Podążaj trasą",
            "Jedź prosto", "Lekko w lewo", "Skręć w lewo", "Ostro w lewo",
            "Lekko w prawo", "Skręć w prawo", "Ostro w prawo", "Zawróć", "Cel", "Podążaj trasą");
        Register("nl",
            "VOOGLE ROUTE", "ROUTE AAN", "ROUTE UIT", "AUTO LOOP", "LOOP AAN", "{0} m",
            "Rechtdoor naar bestemming", "Aankomst", "Bestemming dichtbij", "Volg de route",
            "Rechtdoor", "Licht links", "Sla linksaf", "Scherp links",
            "Licht rechts", "Sla rechtsaf", "Scherp rechts", "Keer om", "Bestemming", "Volg de route");
        Register("tr",
            "VOOGLE ROUTE", "ROTA AÇIK", "ROTA KAPALI", "OTO YÜRÜ", "YÜRÜ AÇIK", "{0} m",
            "Hedefe doğru düz devam edin", "Varış", "Hedef yakında", "Rotayı takip edin",
            "Düz devam", "Hafif sola", "Sola dönün", "Keskin sola",
            "Hafif sağa", "Sağa dönün", "Keskin sağa", "U dönüşü", "Hedef", "Rotayı takip edin");
        Register("ja",
            "VOOGLE ROUTE", "ルート ON", "ルート OFF", "自動歩行", "歩行 ON", "{0} m",
            "目的地まで直進", "到着", "目的地付近", "ルートに従う",
            "直進", "やや左", "左折", "大きく左折",
            "やや右", "右折", "大きく右折", "Uターン", "目的地", "ルートに従う");
        Register("ko",
            "VOOGLE ROUTE", "경로 ON", "경로 OFF", "자동 걷기", "걷기 ON", "{0} m",
            "목적지까지 직진", "도착", "목적지 근처", "경로 따라가기",
            "직진", "약간 좌회전", "좌회전", "급좌회전",
            "약간 우회전", "우회전", "급우회전", "유턴", "목적지", "경로 따라가기");
        Register("zh-CN",
            "VOOGLE ROUTE", "路线 开", "路线 关", "自动步行", "步行 开", "{0} 米",
            "直行前往目的地", "到达", "目的地附近", "沿路线行驶",
            "直行", "稍向左", "左转", "急左转",
            "稍向右", "右转", "急右转", "掉头", "目的地", "沿路线行驶");
        Register("zh-TW",
            "VOOGLE ROUTE", "路線 開", "路線 關", "自動步行", "步行 開", "{0} 公尺",
            "直行前往目的地", "到達", "目的地附近", "沿路線行駛",
            "直行", "稍向左", "左轉", "急左轉",
            "稍向右", "右轉", "急右轉", "迴轉", "目的地", "沿路線行駛");
        Register("cs",
            "VOOGLE ROUTE", "TRASA ZAP", "TRASA VYP", "AUTO CHŮZE", "CHŮZE ZAP", "{0} m",
            "Jeďte rovně k cíli", "Příjezd", "Cíl poblíž", "Sledujte trasu",
            "Jeďte rovně", "Mírně vlevo", "Zahněte vlevo", "Ostrá zatáčka vlevo",
            "Mírně vpravo", "Zahněte vpravo", "Ostrá zatáčka vpravo", "Otočte se", "Cíl", "Sledujte trasu");
        Register("da",
            "VOOGLE ROUTE", "RUTE TIL", "RUTE FRA", "AUTO GÅ", "GÅ TIL", "{0} m",
            "Fortsæt ligeud mod destinationen", "Ankomst", "Destination tæt på", "Følg ruten",
            "Fortsæt ligeud", "Let til venstre", "Drej til venstre", "Skarpt til venstre",
            "Let til højre", "Drej til højre", "Skarpt til højre", "Vend om", "Destination", "Følg ruten");
        Register("fi",
            "VOOGLE ROUTE", "REITTI PÄÄLLÄ", "REITTI POIS", "AUTO KÄVELY", "KÄVELY PÄÄLLÄ", "{0} m",
            "Aja suoraan määränpäähän", "Saapuminen", "Määränpää lähellä", "Seuraa reittiä",
            "Aja suoraan", "Hieman vasemmalle", "Käänny vasemmalle", "Jyrkkä vasen",
            "Hieman oikealle", "Käänny oikealle", "Jyrkkä oikea", "Tee U-käännös", "Määränpää", "Seuraa reittiä");
        Register("el",
            "VOOGLE ROUTE", "ΔΙΑΔΡΟΜΗ ON", "ΔΙΑΔΡΟΜΗ OFF", "ΑΥΤΟ ΠΟΡΕΙΑ", "ΠΟΡΕΙΑ ON", "{0} μ",
            "Συνεχίστε ευθεία προς τον προορισμό", "Άφιξη", "Προορισμός κοντά", "Ακολουθήστε τη διαδρομή",
            "Συνεχίστε ευθεία", "Ελαφρά αριστερά", "Στρίψτε αριστερά", "Απότομα αριστερά",
            "Ελαφρά δεξιά", "Στρίψτε δεξιά", "Απότομα δεξιά", "Αναστροφή", "Προορισμός", "Ακολουθήστε τη διαδρομή");
        Register("hu",
            "VOOGLE ROUTE", "ÚTVONAL BE", "ÚTVONAL KI", "AUTO SÉTA", "SÉTA BE", "{0} m",
            "Haladjon egyenesen a cél felé", "Érkezés", "Cél a közelben", "Kövesse az útvonalat",
            "Haladjon egyenesen", "Kissé balra", "Forduljon balra", "Élesen balra",
            "Kissé jobbra", "Forduljon jobbra", "Élesen jobbra", "Forduljon meg", "Cél", "Kövesse az útvonalat");
        Register("ro",
            "VOOGLE ROUTE", "RUTĂ ON", "RUTĂ OFF", "AUTO MERS", "MERS ON", "{0} m",
            "Continuați drept spre destinație", "Sosire", "Destinație aproape", "Urmați ruta",
            "Continuați drept", "Ușor la stânga", "Virați la stânga", "La stânga strâns",
            "Ușor la dreapta", "Virați la dreapta", "La dreapta strâns", "Întoarceți-vă", "Destinație", "Urmați ruta");
        Register("uk",
            "VOOGLE ROUTE", "МАРШРУТ УВІМК", "МАРШРУТ ВИМК", "АВТО ХОДА", "ХОДА УВІМК", "{0} м",
            "Прямуйте до пункту призначення", "Прибуття", "Пункт поруч", "Слідуйте маршрутом",
            "Прямуйте", "Трохи ліворуч", "Поверніть ліворуч", "Різко ліворуч",
            "Трохи праворуч", "Поверніть праворуч", "Різко праворуч", "Розверніться", "Пункт призначення",
            "Слідуйте маршрутом");
        Register("lt",
            "VOOGLE ROUTE", "MARŠRUTAS ON", "MARŠRUTAS OFF", "AUTO ĖJIMAS", "ĖJIMAS ON", "{0} m",
            "Važiuokite tiesiai į tikslą", "Atvykimas", "Tikslas arti", "Laikykitės maršruto",
            "Važiuokite tiesiai", "Šiek tiek kairėn", "Sukite kairėn", "Aštriai kairėn",
            "Šiek tiek dešinėn", "Sukite dešinėn", "Aštriai dešinėn", "Apsisukite", "Tikslas", "Laikykitės maršruto");
    }

    internal static bool TryGet(string locale, StringKey key, out string text)
    {
        if (TryLocale(locale, key, out text))
            return true;

        var dash = locale.IndexOf('-');
        if (dash > 0 && TryLocale(locale[..dash], key, out text))
            return true;

        return TryLocale("en", key, out text);
    }

    internal static string GetEnglish(StringKey key) =>
        TryLocale("en", key, out var text) ? text : key.ToString();

    private static bool TryLocale(string locale, StringKey key, out string text)
    {
        text = "";
        return ByLocale.TryGetValue(locale, out var table) && table.TryGetValue(key, out text!);
    }

    private static void Register(
        string locale,
        string panelTitle,
        string routeOn,
        string routeOff,
        string autoWalk,
        string walkOn,
        string metersFormat,
        string continueStraightToDestination,
        string arrival,
        string destinationNear,
        string followRoute,
        string turnStraight,
        string turnSlightLeft,
        string turnLeft,
        string turnSharpLeft,
        string turnSlightRight,
        string turnRight,
        string turnSharpRight,
        string turnUTurn,
        string turnArrival,
        string turnFollowRoute)
    {
        var table = new Dictionary<StringKey, string>
        {
            [StringKey.PanelTitle] = panelTitle,
            [StringKey.RouteOn] = routeOn,
            [StringKey.RouteOff] = routeOff,
            [StringKey.AutoWalk] = autoWalk,
            [StringKey.WalkOn] = walkOn,
            [StringKey.MetersFormat] = metersFormat,
            [StringKey.ContinueStraightToDestination] = continueStraightToDestination,
            [StringKey.Arrival] = arrival,
            [StringKey.DestinationNear] = destinationNear,
            [StringKey.FollowRoute] = followRoute,
            [StringKey.TurnStraight] = turnStraight,
            [StringKey.TurnSlightLeft] = turnSlightLeft,
            [StringKey.TurnLeft] = turnLeft,
            [StringKey.TurnSharpLeft] = turnSharpLeft,
            [StringKey.TurnSlightRight] = turnSlightRight,
            [StringKey.TurnRight] = turnRight,
            [StringKey.TurnSharpRight] = turnSharpRight,
            [StringKey.TurnUTurn] = turnUTurn,
            [StringKey.TurnArrival] = turnArrival,
            [StringKey.TurnFollowRoute] = turnFollowRoute,
        };
        SettingsStrings.MergeInto(table, locale);
        ByLocale[locale] = table;
    }
}
