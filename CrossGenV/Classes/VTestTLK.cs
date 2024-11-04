using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.TLK.ME1;
using LegendaryExplorerCore.TLK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossGenV.Classes
{
    /// <summary>
    /// TLK-specific things
    /// </summary>
    public static class VTestTLK
    {
        private static void AddVTestSpecificStrings(ExportEntry tlkExport, string lang)
        {
            ME1TalkFile talkF = new ME1TalkFile(tlkExport);
            var stringRefs = talkF.StringRefs.ToList();
            // LANGUAGE SPECIFIC STRINGS HERE
            switch (lang)
            {
                case "RA":
                case "RU":
                    // Localization by Cole Phelps
                    stringRefs.Add(new TLKStringRef(338464, "Загрузка данных"));
                    stringRefs.Add(new TLKStringRef(338465, "Настройки симулятора"));
                    stringRefs.Add(new TLKStringRef(338466,
                        "Открыть")); // Context: Activatable console to open the simulator settings
                    stringRefs.Add(new TLKStringRef(338467, "АКТИВИРОВАНО"));
                    stringRefs.Add(new TLKStringRef(338468, "ДЕАКТИВИРОВАНО"));
                    stringRefs.Add(new TLKStringRef(338469, "Активировано"));
                    stringRefs.Add(new TLKStringRef(338470, "Деактивировано"));
                    stringRefs.Add(new TLKStringRef(338471, "Музыка"));
                    stringRefs.Add(new TLKStringRef(338472,
                        "К каждой карте симулятора будут добавлены подходящие музыкальные треки, которые будут меняться по мере нарастания напряжения."));
                    stringRefs.Add(new TLKStringRef(338473,
                        "Как и в оригинальном DLC, во время симуляция не будет никакой музыки."));
                    stringRefs.Add(new TLKStringRef(338474, "ОП за 1-е место"));
                    stringRefs.Add(new TLKStringRef(338475,
                        "Вы получаете опыт за 1-е место на карте симуляции. За 1-е место на каждой карте симулятора начисляется треть ОП, необходимых для повышения уровня. Дополнительные ОП будут начисляться за побитый рекорд, а также за прохождение особого сценария."));
                    stringRefs.Add(
                        new TLKStringRef(338476, "Опыт не будет начисляться за прохождение карт симулятора."));
                    stringRefs.Add(new TLKStringRef(338477, "Выживание: Повышение количества противника"));
                    stringRefs.Add(new TLKStringRef(338478, "Больше противника"));
                    stringRefs.Add(new TLKStringRef(338479,
                        "Количество противника будет фиксированным, как и в оригинальной версии."));
                    stringRefs.Add(new TLKStringRef(338480,
                        "Количество противника в выживании будет расти со временем, из-за чего сложность будет расти по кривой по мере прохождения."));
                    stringRefs.Add(new TLKStringRef(338481, "Рост сложности: Таланты"));
                    stringRefs.Add(new TLKStringRef(338482,
                        "По мере прохождения миссий противник получает таланты и способности,  которые делают его более смертоносным."));
                    stringRefs.Add(new TLKStringRef(338483,
                        "Противник не будет получать таланты по мере прохождения миссий симуляции. Опция по умолчанию."));
                    stringRefs.Add(new TLKStringRef(338484, "Рост сложности: Оружие"));
                    stringRefs.Add(new TLKStringRef(338485,
                        "По мере прохождения миссий противник получает модификации оружия, которые делают его более смертоносным."));
                    stringRefs.Add(new TLKStringRef(338486,
                        "Противник не будет получать модификации оружия по мере прохождения. Опция по умолчанию."));
                    stringRefs.Add(new TLKStringRef(338487, "Симулятор: Селектор противника"));
                    stringRefs.Add(new TLKStringRef(338488, "Выберите тип противника"));
                    stringRefs.Add(new TLKStringRef(338489, "Этот сет противников не активирован."));
                    stringRefs.Add(new TLKStringRef(338490, "Противники-азари заменят обычных в боевой симуляции."));
                    stringRefs.Add(
                        new TLKStringRef(338491, "Противники-саларианцы заменят обычных в боевой симуляции."));
                    stringRefs.Add(
                        new TLKStringRef(338492, "Противники-батарианцы заменят обычных в боевой симуляции."));
                    stringRefs.Add(new TLKStringRef(338493, "Монстры"));
                    stringRefs.Add(new TLKStringRef(338494,
                        "Монструозные напарники заменят обычных в боевой симуляции."));
                    stringRefs.Add(new TLKStringRef(338495, "Смешанные"));
                    stringRefs.Add(new TLKStringRef(338496,
                        "Противнпки из всех доступных фракций появятся в боевой симуляции."));
                    stringRefs.Add(new TLKStringRef(338497, "Все настройки: По умолчанию"));
                    stringRefs.Add(new TLKStringRef(338498, "Все настройки"));
                    stringRefs.Add(new TLKStringRef(338499,
                        "Все настройки будут переключены на опции из оригинала. Для сторонников чистой «Станции „Вершина“»."));
                    stringRefs.Add(new TLKStringRef(338500, "Все настройки: Рекомендованные"));
                    stringRefs.Add(new TLKStringRef(338501,
                        "Все настройки будут переключены на опции, рекомендованные для этого ремастера."));
                    stringRefs.Add(new TLKStringRef(338502, "Выбрано"));
                    stringRefs.Add(new TLKStringRef(338503, "Не выбрано"));
                    stringRefs.Add(new TLKStringRef(338504, "Тип противника не изменен."));
                    stringRefs.Add(new TLKStringRef(338505, "Тип противника был изменен"));
                    stringRefs.Add(new TLKStringRef(338506, "По умолчанию"));
                    break;
                case "ES":
                    AddEnglishStringRefs();
                    break;
                case "IT":
                    stringRefs.Add(new TLKStringRef(338464, "Scaricamento Dati"));
                    stringRefs.Add(new TLKStringRef(338465, "Impostazioni Simulatore"));
                    stringRefs.Add(new TLKStringRef(338466,
                        "Apri")); // Context: Activatable console to open the simulator settings
                    stringRefs.Add(new TLKStringRef(338467, "ABILITATO"));
                    stringRefs.Add(new TLKStringRef(338468, "DISABILITATO"));
                    stringRefs.Add(new TLKStringRef(338469, "Abilita"));
                    stringRefs.Add(new TLKStringRef(338470, "Disabilita"));
                    stringRefs.Add(new TLKStringRef(338471, "Musica"));
                    stringRefs.Add(new TLKStringRef(338472,
                        "Tracce misucali appropriate saranno aggiunte a ciascuna mappa del simulatore, e cambieranno con l'aumentare dell'intensità."));
                    stringRefs.Add(new TLKStringRef(338473,
                        "Come nel DLC originale, non sarà riprodotta alcuna misuca all'interno del simulatore."));
                    stringRefs.Add(new TLKStringRef(338474, "XP al 1o Posto"));
                    stringRefs.Add(new TLKStringRef(338475,
                        "Riceverai esperienza dopo aver raggiunto il 1o posto in una mappa del simulatore. Riceverai 1/3 degli XP totali di un livello appena raggiungerai il 1o posto per la prima volta in ciascuna mappa del simulatore. XP aggiuntivi ti saranno assegnati se batterai un tuo record, o al completamento di uno scenario speciale."));
                    stringRefs.Add(new TLKStringRef(338476,
                        "Non riceverai alcuna esperienza al completamento di una mappa del simulatore."));
                    stringRefs.Add(new TLKStringRef(338477, "Sopravvivenza: Aumento del Numero di Nemici"));
                    stringRefs.Add(new TLKStringRef(338478, "Aumento Nemici"));
                    stringRefs.Add(new TLKStringRef(338479,
                        "Il numero di amici generati è fisso al numero di default, come da versione originale."));
                    stringRefs.Add(new TLKStringRef(338480,
                        "Il numero di nemici i nmodalità sopravvivenza aumenterà nel tempo, creando una curva di difficoltà crescente col tempo."));
                    stringRefs.Add(new TLKStringRef(338481, "Aumento Difficoltà: Talenti"));
                    stringRefs.Add(new TLKStringRef(338482,
                        "Mano a mano che la missione progredisce, i nemici guadagneranno talenti e poteri che li renderanno più letali."));
                    stringRefs.Add(new TLKStringRef(338483,
                        "I nemici non guadagneranno talenti col progredire delle missioni del simulatore. Questo è il valore predefinito."));
                    stringRefs.Add(new TLKStringRef(338484, "Aumento Difficoltà: Armi"));
                    stringRefs.Add(new TLKStringRef(338485,
                        "Mano a mano che la missione progredisce, i nemimci guadagneranno mod per le loro armi che le renderanno più letali."));
                    stringRefs.Add(new TLKStringRef(338486,
                        "I nemici non guadagneranno mod per le loro armicol progredire delle missioni del simulatore. Questo è il valore predefinito."));
                    stringRefs.Add(new TLKStringRef(338487, "Simulatore: Selettore Nemici"));
                    stringRefs.Add(new TLKStringRef(338488, "Seleziona il tipo di nemici"));
                    stringRefs.Add(new TLKStringRef(338489, "Questo set di nemici è al momento disabilitato."));
                    stringRefs.Add(new TLKStringRef(338490,
                        "Nemici Asari sostituiranno i nemici normali nel simulatore di compabbimento."));
                    stringRefs.Add(new TLKStringRef(338491,
                        "Nemici Salarian sostituiranno i nemici normali nel simulatore di compabbimento."));
                    stringRefs.Add(new TLKStringRef(338492,
                        "Nemici Batarian sostituiranno i nemici normali nel simulatore di compabbimento."));
                    stringRefs.Add(new TLKStringRef(338493, "Mostri"));
                    stringRefs.Add(new TLKStringRef(338494,
                        "Nemici mostruosi sostituiranno i nemici normali nel simulatore di compabbimento."));
                    stringRefs.Add(new TLKStringRef(338495, "Nemici Misti"));
                    stringRefs.Add(new TLKStringRef(338496,
                        "Nemici di tutte le fazioni sostituiranno i nemici normali nel simulatore di compabbimento."));
                    stringRefs.Add(new TLKStringRef(338497, "Tutte le impostazioni: Vanilla"));
                    stringRefs.Add(new TLKStringRef(338498, "Tutte le impostazioni"));
                    stringRefs.Add(new TLKStringRef(338499,
                        "Tutte le impostazioni del simulatore saranno impostate al loro valore vanilla originale di Mass Effect 1. Per i puristi di Stazione Pinnacle,"));
                    stringRefs.Add(new TLKStringRef(338500, "Tutte le impostazioni: Consigliate"));
                    stringRefs.Add(new TLKStringRef(338501,
                        "Tutte le impostazioni del simulatore saranno impostate al loro valore consigliato per questa remaster."));
                    stringRefs.Add(new TLKStringRef(338502, "Selezionato"));
                    stringRefs.Add(new TLKStringRef(338503, "Non selezionato"));
                    stringRefs.Add(new TLKStringRef(338504,
                        "I tipi di nemici non saranno cambiati rispetto ai predefiniti."));
                    stringRefs.Add(new TLKStringRef(338505,
                        "I tipi di nemici sono stati cambiati rispetto ai predefiniti."));
                    stringRefs.Add(new TLKStringRef(338506, "Predefiniti"));
                    break;
                case "PL":
                case "PLPC":
                    // Localization by Gatzek
                    stringRefs.Add(new TLKStringRef(338464, "Pobieranie danych"));
                    stringRefs.Add(new TLKStringRef(338465, "Ustawienia symulatora"));
                    stringRefs.Add(new TLKStringRef(338466,
                        "Otwórz")); // Context: Activatable console to open the simulator settings
                    stringRefs.Add(new TLKStringRef(338467, "WŁĄCZONE"));
                    stringRefs.Add(new TLKStringRef(338468, "WYŁĄCZONE"));
                    stringRefs.Add(new TLKStringRef(338469, "Włącz"));
                    stringRefs.Add(new TLKStringRef(338470, "Wyłącz"));
                    stringRefs.Add(new TLKStringRef(338471, "Muzyka"));
                    stringRefs.Add(new TLKStringRef(338472,
                        "Do każdej mapy symulatora zostaną dodane odpowiednie ścieżki muzyczne, które będą się zmieniać w miarę wzrostu intensywności rozgrywki."));
                    stringRefs.Add(new TLKStringRef(338473,
                        "Podobnie jak w oryginalnym DLC, w symulatorze nie będzie odtwarzana żadna muzyka."));
                    stringRefs.Add(new TLKStringRef(338474, "PD za 1. miejsce"));
                    stringRefs.Add(new TLKStringRef(338475,
                        "Doświadczenie zdobywasz, gdy zajmiesz 1. miejsce na mapie symulatora. Za pierwszym razem, gdy zdobędziesz 1. miejsce na każdej mapie symulatora, otrzymasz 1/3 PD. Dodatkowe PD zdobywa się za pobicie swojego rekordu i ukończenie specjalnego scenariusza."));
                    stringRefs.Add(new TLKStringRef(338476,
                        "Po ukończeniu mapy symulatora nie zostanie przyznane żadne doświadczenie."));
                    stringRefs.Add(new TLKStringRef(338477, "Przetrwanie: Liczba wrogów wzrasta"));
                    stringRefs.Add(new TLKStringRef(338478, "Wzrost liczby wrogów"));
                    stringRefs.Add(new TLKStringRef(338479,
                        "Liczba pojawiających się wrogów zostanie ustalona na poziomie domyślnym, oryginalnym."));
                    stringRefs.Add(new TLKStringRef(338480,
                        "Liczba wrogów w trybie przetrwania będzie się z czasem zwiększać, co spowoduje wzrost poziomu trudności rozgrywki."));
                    stringRefs.Add(new TLKStringRef(338481, "Zwiększ poziom trudności: Talenty"));
                    stringRefs.Add(new TLKStringRef(338482,
                        "W miarę postępów misji wrogowie będą zdobywać talenty i moce, które uczynią ich bardziej zabójczymi."));
                    stringRefs.Add(new TLKStringRef(338483,
                        "Wrogowie nie będą zdobywać talentów w miarę postępu misji w symulatorze. Jest to wartość domyślna."));
                    stringRefs.Add(new TLKStringRef(338484, "Zwiększ poziom trudności: Bronie"));
                    stringRefs.Add(new TLKStringRef(338485,
                        "W miarę postępów misji przeciwnicy będą zdobywać ulepszenia broni, które uczynią ją bardziej zabójczą."));
                    stringRefs.Add(new TLKStringRef(338486,
                        "Wrogowie nie będą zdobywać modyfikacji broni w miarę postępu misji w symulatorze. Jest to wartość domyślna."));
                    stringRefs.Add(new TLKStringRef(338487, "Symulator: Wybór wroga"));
                    stringRefs.Add(new TLKStringRef(338488, "Wybierz typ wroga"));
                    stringRefs.Add(new TLKStringRef(338489, "Ten zestaw wrogów nie jest obecnie włączony."));
                    stringRefs.Add(new TLKStringRef(338490, "W symulatorze walki normalnych wrogów zastąpią asari."));
                    stringRefs.Add(new TLKStringRef(338491,
                        "W symulatorze walki normalnych wrogów zastąpią salarianie."));
                    stringRefs.Add(new TLKStringRef(338492,
                        "W symulatorze walki normalnych wrogów zastąpią batarianie."));
                    stringRefs.Add(new TLKStringRef(338493, "Potwory"));
                    stringRefs.Add(new TLKStringRef(338494, "W symulatorze walki normalnych wrogów zastąpią potwory."));
                    stringRefs.Add(new TLKStringRef(338495, "Mieszani wrogowie"));
                    stringRefs.Add(new TLKStringRef(338496,
                        "W symulatorze walki pojawią się przeciwnicy ze wszystkich dostępnych frakcji."));
                    stringRefs.Add(new TLKStringRef(338497, "Wszystkie ustawienia: vanilla"));
                    stringRefs.Add(new TLKStringRef(338498, "Wszystkie ustawienia"));
                    stringRefs.Add(new TLKStringRef(338499,
                        "Wszystkie ustawienia symulatora zostaną ustawione na oryginalną wartość Mass Effect 1. Dla purystów Pinnacle Station."));
                    stringRefs.Add(new TLKStringRef(338500, "Wszystkie ustawienia: Zalecane"));
                    stringRefs.Add(new TLKStringRef(338501,
                        "Wszystkie ustawienia symulatora zostaną przywrócone do stanu zalecanego dla tego moda."));
                    stringRefs.Add(new TLKStringRef(338502, "Wybrane"));
                    stringRefs.Add(new TLKStringRef(338503, "Nie wybrane"));
                    stringRefs.Add(new TLKStringRef(338504,
                        "Typy wrogów nie ulegną zmianie w stosunku do ustawień domyślnych."));
                    stringRefs.Add(new TLKStringRef(338505, "Zmieniono domyślne typy wrogów."));
                    stringRefs.Add(new TLKStringRef(338506, "Domyślne"));
                    break;
                case "FR":
                    // Localization by karolie
                    stringRefs.Add(new TLKStringRef(338464, "Téléchargement des données"));
                    stringRefs.Add(new TLKStringRef(338465, "Paramètres du simulateur"));
                    stringRefs.Add(new TLKStringRef(338466, "Ouvrir"));
                    stringRefs.Add(new TLKStringRef(338467, "ACTIVE"));
                    stringRefs.Add(new TLKStringRef(338468, "INACTIVE"));
                    stringRefs.Add(new TLKStringRef(338469, "Activer"));
                    stringRefs.Add(new TLKStringRef(338470, "Désactiver"));
                    stringRefs.Add(new TLKStringRef(338471, "Musique"));
                    stringRefs.Add(new TLKStringRef(338472,
                        "De la musique appropriée sera jouée à chaque niveau du simulateur, et elle changera lorsque l'intensité augmentera."));
                    stringRefs.Add(new TLKStringRef(338473,
                        "Aucune musique ne sera jouée dans le simulateur, comme c'était le cas dans le jeu original."));
                    stringRefs.Add(new TLKStringRef(338474, "XP à la première place"));
                    stringRefs.Add(new TLKStringRef(338475,
                        "Vous gagnerez de l'expérience en atteignant la 1re place d'un niveau du simulateur. Vous receverez 1/3 de la valeur des points d'un niveau en atteignant la première place pour la première fois à chaque niveau. Des XP additionnels vous seront attribués lorsque vous battez votre record, et lorsque vous terminez un scénario spécial."));
                    stringRefs.Add(new TLKStringRef(338476,
                        "Vous ne receverez aucune expérience en complétant un niveau du simulateur."));
                    stringRefs.Add(new TLKStringRef(338477, "Survie : le nombre d'ennemis augmente"));
                    stringRefs.Add(new TLKStringRef(338478, "Augmentation du nombre d'ennemis"));
                    stringRefs.Add(new TLKStringRef(338479,
                        "Le nombre d'ennemis qui apparaîtra sera fixé au même que celui du jeu original."));
                    stringRefs.Add(new TLKStringRef(338480,
                        "Le nombre d'ennemis du mode survie va augmenter avec le temps, pour créer une courbe d'augmentation de la difficulté au fil du temps."));
                    stringRefs.Add(new TLKStringRef(338481, "Augmentation de la difficulté : talents"));
                    stringRefs.Add(new TLKStringRef(338482,
                        "Au fil d'une mission, les ennemis vont gagner des talents et pouvoirs qui les rendent plus mortels."));
                    stringRefs.Add(new TLKStringRef(338483,
                        "Les ennemis ne gagneront pas de talents au fil des missions du simulateur (valeur par défaut)."));
                    stringRefs.Add(new TLKStringRef(338484, "Augmentation de la difficulté : armes"));
                    stringRefs.Add(new TLKStringRef(338485,
                        "Au fil d'une mission, les ennemis gagneront des modificateurs d'armes qui les rendent plus mortels."));
                    stringRefs.Add(new TLKStringRef(338486,
                        "Les ennemis ne gagneront pas de modificateurs d'armes au fil des missions du simulateur (valeur par défaut)."));
                    stringRefs.Add(new TLKStringRef(338487, "Simulateur : sélection d'ennemis"));
                    stringRefs.Add(new TLKStringRef(338488, "Sélectionner le type d'ennemis"));
                    stringRefs.Add(new TLKStringRef(338489, "Ce type d'ennemis est présentement désactivé."));
                    stringRefs.Add(new TLKStringRef(338490,
                        "Des ennemis Asari vont remplacer les ennemis normaux du simulateur de combat."));
                    stringRefs.Add(new TLKStringRef(338491,
                        "Des ennemis Galariens vont remplacer les ennemis normaux du simulateur de combat."));
                    stringRefs.Add(new TLKStringRef(338492,
                        "Des ennemis Butariens vont remplacer les ennemis normaux du simulateur de combat."));
                    stringRefs.Add(new TLKStringRef(338493, "Monstres"));
                    stringRefs.Add(new TLKStringRef(338494,
                        "Des ennemis monstres vont remplacer les ennemis normaux du simulateur de combat."));
                    stringRefs.Add(new TLKStringRef(338495, "Ennemis mélangés"));
                    stringRefs.Add(new TLKStringRef(338496,
                        "Des ennemis de toutes les factions apparaîtront dans le simulateur de combat."));
                    stringRefs.Add(new TLKStringRef(338497, "Tous les paramètres : par défaut"));
                    stringRefs.Add(new TLKStringRef(338498, "Tous les paramètres"));
                    stringRefs.Add(new TLKStringRef(338499,
                        "Tous les paramètres du simulateur seront mis aux valeurs par défaut, comme dans le jeu original. Pour les puristes de la Pinnacle Station."));
                    stringRefs.Add(new TLKStringRef(338500, "Tous les paramètres : recommandés"));
                    stringRefs.Add(new TLKStringRef(338501,
                        "Tous les paramètres du simulateur seront mis aux valeurs recommandées pour cette version remasterisée."));
                    stringRefs.Add(new TLKStringRef(338502, "Sélectionné"));
                    stringRefs.Add(new TLKStringRef(338503, "Pas sélectionné"));
                    stringRefs.Add(new TLKStringRef(338504,
                        "Le type d'ennemis ne changera pas de la valeur par défaut."));
                    stringRefs.Add(new TLKStringRef(338505, "Le type d'ennemis a été changé de la valeur par défaut."));
                    stringRefs.Add(new TLKStringRef(338506, "Par défaut"));
                    break;
                case "DE":
                    // Localization by Herobrine24
                    stringRefs.Add(new TLKStringRef(338464, "Herunterladen von Daten"));
                    stringRefs.Add(new TLKStringRef(338465, "Simulatoreinstellungen"));
                    stringRefs.Add(new TLKStringRef(338466,
                        "Öffnen")); // Context: Activatable console to open the simulator settings
                    stringRefs.Add(new TLKStringRef(338467, "AKTIVIERT"));
                    stringRefs.Add(new TLKStringRef(338468, "DEAKTIVIERT"));
                    stringRefs.Add(new TLKStringRef(338469, "Aktivieren"));
                    stringRefs.Add(new TLKStringRef(338470, "Deaktivieren"));
                    stringRefs.Add(new TLKStringRef(338471, "Musik"));
                    stringRefs.Add(new TLKStringRef(338472,
                        "Zu jeder Simulatorkarte werden passende Musiktitel hinzugefügt, die sich mit zunehmender Intensität ändern."));
                    stringRefs.Add(new TLKStringRef(338473,
                        "Wie im Original-DLC wird im Simulator keine Musik abgespielt."));
                    stringRefs.Add(new TLKStringRef(338474, "XP auf dem 1. Platz"));
                    stringRefs.Add(new TLKStringRef(338475,
                        "Sie erhalten Erfahrung, wenn Sie auf einer Simulatorkarte den 1. Platz erreichen. Wenn Sie auf jeder Simulatorkarte zum ersten Mal den 1. Platz erreichen, erhalten Sie 1/3 der XP eines Levels. Weitere XP erhalten Sie, wenn Sie Ihren Rekord schlagen und das Spezialszenario abschließen."));
                    stringRefs.Add(new TLKStringRef(338476,
                        "Für das Abschließen einer Simulatorkarte wird keine Erfahrung gewährt."));
                    stringRefs.Add(new TLKStringRef(338477, "Überleben: Anstieg der Feindzahl"));
                    stringRefs.Add(new TLKStringRef(338478, "Anstieg der Feindzahl"));
                    stringRefs.Add(new TLKStringRef(338479,
                        "Die Anzahl der erscheinenden Feinde wird auf die ursprüngliche Standardversion festgelegt."));
                    stringRefs.Add(new TLKStringRef(338480,
                        "Die Zahl der Feinde im Überlebensmodus nimmt mit der Zeit zu, wodurch mit fortschreitender Zeit eine zunehmende Schwierigkeitskurve entsteht."));
                    stringRefs.Add(new TLKStringRef(338481, "Anstieg Schwierigkeit: Talente"));
                    stringRefs.Add(new TLKStringRef(338482,
                        "Im Verlauf einer Mission erhalten die Feinde Talente und Kräfte, die sie tödlicher machen."));
                    stringRefs.Add(new TLKStringRef(338483,
                        "Feinde erhalten im Verlauf der Simulatormissionen keine Talente. Dies ist der Standardwert."));
                    stringRefs.Add(new TLKStringRef(338484, "Anstieg Schwierigkeit: Waffen"));
                    stringRefs.Add(new TLKStringRef(338485,
                        "Im Verlauf einer Mission erhalten Feinde Waffenmodifikationen, die sie tödlicher machen."));
                    stringRefs.Add(new TLKStringRef(338486,
                        "Feinde erhalten im Verlauf der Simulatormissionen keine Waffenmodifikationen. Dies ist der Standardwert."));
                    stringRefs.Add(new TLKStringRef(338487, "Simulator: Feindauswahl"));
                    stringRefs.Add(new TLKStringRef(338488, "Feindtyp auswählen"));
                    stringRefs.Add(new TLKStringRef(338489, "Diese Feindgruppe ist derzeit nicht aktiviert."));
                    stringRefs.Add(new TLKStringRef(338490,
                        "Asari-Feinde werden die normalen Feinde im Kampfsimulator ersetzen."));
                    stringRefs.Add(new TLKStringRef(338491,
                        "Salarianische Feinde werden die normalen Feinde im Kampfsimulator ersetzen."));
                    stringRefs.Add(new TLKStringRef(338492,
                        "Batarianische Feinde werden die normalen Feinde im Kampfsimulator ersetzen."));
                    stringRefs.Add(new TLKStringRef(338493, "Monster"));
                    stringRefs.Add(new TLKStringRef(338494,
                        "Monströse Feinde werden die normalen Feinde im Kampfsimulator ersetzen."));
                    stringRefs.Add(new TLKStringRef(338495, "Gemischte Feinde"));
                    stringRefs.Add(new TLKStringRef(338496,
                        "Feinde aller verfügbaren Fraktionen werden im Kampfsimulator erscheinen."));
                    stringRefs.Add(new TLKStringRef(338497, "Alle Einstellungen: Vanilla"));
                    stringRefs.Add(new TLKStringRef(338498, "Alle Einstellungen"));
                    stringRefs.Add(new TLKStringRef(338499,
                        "Alle Simulatoreinstellungen werden auf ihren ursprünglichen Mass Effect 1-Standardwert eingestellt. Für Pinnacle Station-Puristen."));
                    stringRefs.Add(new TLKStringRef(338500, "Alle Einstellungen: Empfohlen"));
                    stringRefs.Add(new TLKStringRef(338501,
                        "Alle Simulatoreinstellungen werden für diesen Remaster auf die empfohlenen Zustände gesetzt."));
                    stringRefs.Add(new TLKStringRef(338502, "Ausgewählt"));
                    stringRefs.Add(new TLKStringRef(338503, "Nicht ausgewählt"));
                    stringRefs.Add(new TLKStringRef(338504,
                        "Die Feindtypen werden gegenüber den Standardeinstellungen nicht geändert."));
                    stringRefs.Add(new TLKStringRef(338505,
                        "Die Feindtypen wurden gegenüber den Standardeinstellungen geändert."));
                    stringRefs.Add(new TLKStringRef(338506, "Standard"));
                    break;
                case "JA":
                    AddEnglishStringRefs();
                    break;
            }
            
            var huff = new HuffmanCompression();
            huff.LoadInputData(stringRefs);
            huff.SerializeTalkfileToExport(tlkExport);

            void AddEnglishStringRefs()
            {
                stringRefs.Add(new TLKStringRef(338464, "Downloading Data"));
                stringRefs.Add(new TLKStringRef(338465, "Simulator Settings"));
                stringRefs.Add(new TLKStringRef(338466, "Open"));
                stringRefs.Add(new TLKStringRef(338467, "ENABLED"));
                stringRefs.Add(new TLKStringRef(338468, "DISABLED"));
                stringRefs.Add(new TLKStringRef(338469, "Enable"));
                stringRefs.Add(new TLKStringRef(338470, "Disable"));
                stringRefs.Add(new TLKStringRef(338471, "Music"));
                stringRefs.Add(new TLKStringRef(338472, "Appropriate music tracks will be added to each simulator map, which will change as the intensity ramps up."));
                stringRefs.Add(new TLKStringRef(338473, "As in the original DLC, no music will be played in the simulator."));
                stringRefs.Add(new TLKStringRef(338474, "XP on 1st Place"));
                stringRefs.Add(new TLKStringRef(338475, "You will be granted experience upon getting 1st place in a simulator map. 1/3 of a level's worth of XP will be granted upon achieving 1st place for the first time on each simulator map. Additional XP will be granted upon beating your record, and upon completing the special scenario."));
                stringRefs.Add(new TLKStringRef(338476, "No experience will be granted upon completing a simulator map."));
                stringRefs.Add(new TLKStringRef(338477, "Survival: Enemy Count Ramping"));
                stringRefs.Add(new TLKStringRef(338478, "Enemy Ramping"));
                stringRefs.Add(new TLKStringRef(338479, "The amount of enemies that spawn will be fixed to the default, original version."));
                stringRefs.Add(new TLKStringRef(338480, "The amount of enemies in survival mode will increase over time, creating a increasing difficulty curve as time progresses."));
                stringRefs.Add(new TLKStringRef(338481, "Difficulty Ramping: Talents"));
                stringRefs.Add(new TLKStringRef(338482, "As a mission progresses, enemies will gain talents and powers that make them more lethal."));
                stringRefs.Add(new TLKStringRef(338483, "Enemies will not gain talents as simulator missions progress. This is the default value."));
                stringRefs.Add(new TLKStringRef(338484, "Difficulty Ramping: Weapons"));
                stringRefs.Add(new TLKStringRef(338485, "As a mission progresses, enemies will gain weapon mods that make them more lethal."));
                stringRefs.Add(new TLKStringRef(338486, "Enemies will not gain weapon mods as simulator missions progress. This is the default value."));
                stringRefs.Add(new TLKStringRef(338487, "Simulator: Enemy Selector"));
                stringRefs.Add(new TLKStringRef(338488, "Select enemy type"));
                stringRefs.Add(new TLKStringRef(338489, "This set of enemies is currently not enabled."));
                stringRefs.Add(new TLKStringRef(338490, "Asari enemies will replace the normal enemies in the combat simulator."));
                stringRefs.Add(new TLKStringRef(338491, "Salarian enemies will replace the normal enemies in the combat simulator."));
                stringRefs.Add(new TLKStringRef(338492, "Batarian enemies will replace the normal enemies in the combat simulator."));
                stringRefs.Add(new TLKStringRef(338493, "Monsters"));
                stringRefs.Add(new TLKStringRef(338494, "Monsterous enemies will replace the normal enemies in the combat simulator."));
                stringRefs.Add(new TLKStringRef(338495, "Mixed Enemies"));
                stringRefs.Add(new TLKStringRef(338496, "Enemies from all available factions will appear in the combat simulator."));
                stringRefs.Add(new TLKStringRef(338497, "All Settings: Vanilla"));
                stringRefs.Add(new TLKStringRef(338498, "All Settings"));
                stringRefs.Add(new TLKStringRef(338499, "All simulator settings will be set to their vanilla, original Mass Effect 1 value. For Pinnacle Station purists."));
                stringRefs.Add(new TLKStringRef(338500, "All Settings: Recommended"));
                stringRefs.Add(new TLKStringRef(338501, "All simulator settings will be set to their recommended states for this remaster."));
                stringRefs.Add(new TLKStringRef(338502, "Selected"));
                stringRefs.Add(new TLKStringRef(338503, "Not Selected"));
                stringRefs.Add(new TLKStringRef(338504, "Enemy types will not be changed from the defaults."));
                stringRefs.Add(new TLKStringRef(338505, "Enemy types have been changed from the defaults."));
                stringRefs.Add(new TLKStringRef(338506, "Default"));
            }
        }

        public static void PostUpdateTLKs(VTestOptions vTestOptions)
        {
            var basePath = Path.Combine(VTestPaths.VTest_FinalDestDir, "DLC_MOD_Vegas_GlobalTlk_");
            var langsToUpdate = new[] { "RA", "RU", "DE", "FR", "IT", "ES", "JA", "PL", "PLPC" };
            foreach (var lang in langsToUpdate)
            {
                var tlkPackage = MEPackageHandler.OpenMEPackage(basePath + lang + ".pcc");

                // Add our specific TLK strings.
                AddVTestSpecificStrings(tlkPackage.FindExport("GlobalTlk_tlk"), lang);
                AddVTestSpecificStrings(tlkPackage.FindExport("GlobalTlk_tlk_M"), lang);

                tlkPackage.Save();

                // Add English VO TLKs
                switch (lang)
                {
                    case "DE":
                        tlkPackage.Save(basePath + "GE.pcc"); // English VO
                        break;
                    case "FR":
                        tlkPackage.Save(basePath + "FE.pcc"); // English VO
                        break;
                    case "IT":
                        tlkPackage.Save(basePath + "IE.pcc"); // English VO
                        break;
                }
            }
        }
    }
}
