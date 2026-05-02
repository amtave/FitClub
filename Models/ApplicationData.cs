using System.Collections.Generic;

namespace FitClub.Models
{
    public static class TrainerSpecializations
    {
        public static List<string> GetAll()
        {
            return new List<string>
            {
                "Кардио, функциональный тренинг",
                "Силовой тренинг, бодибилдинг",
                "Йога, пилатес, стретчинг",
                "Функциональный тренинг, кроссфит",
                "Кардио, аэробика, танцевальная",
                "Единоборства, функциональный тренинг",
                "Другое"
            };
        }
    }

    public static class DesiredSchedules
    {
        public static List<(string Value, string Display)> GetAll()
        {
            return new List<(string, string)>
            {
                ("fulltime", "Полный день"),
                ("shift", "Сменный график"),
                ("morning", "Только утро"),
                ("evening", "Только вечер"),
                ("weekend", "Выходного дня")
            };
        }
    }

    public static class AgeGroups
    {
        public static List<(string Value, string Display)> GetAll()
        {
            return new List<(string, string)>
            {
                ("children", "Дети (6-12 лет)"),
                ("teens", "Подростки (13-17 лет)"),
                ("adults", "Взрослые (18-45 лет)"),
                ("seniors", "Старшее поколение (45+)")
            };
        }
    }

    public static class PersonalQualities
    {
        public static List<(string Name, string Description)> GetAll()
        {
            return new List<(string, string)>
            {
                ("Дисциплинированность", "Соблюдение правил и расписания"),
                ("Коммуникабельность", "Умение находить общий язык с людьми"),
                ("Ответственность", "Готовность отвечать за результаты своей работы"),
                ("Эмпатия к клиентам", "Понимание чувств и потребностей клиентов")
            };
        }
    }
}