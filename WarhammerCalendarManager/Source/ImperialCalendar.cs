﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace WarhammerCalendarManager
{
    public enum moonPhase {newMoon, waxingCrsec, firstQuarter, waxingGib, full, waningGib, lastQuarter, waningCresc};
    public class ImperialCalendar
    {


        #region jsonData

        public string calendarName { get; set; }

        readonly static int numDaysInYear = 400;


        readonly static int numMonthsInYear = 12;

        readonly static string[] intercalaryHolidays = { null, "Hexentag", null, "Mitterfruhl", null, null, "Sonnstill", "Geheimnistag", null, "Mittherbst", null, null, "Monstille" };
        readonly static string[] intercalaryAltHolidays = { null, "Witching Day", null, "Start Growth", null, null, "Sun Still", "Day of Mystery", null, "Less Growth", null, null, "World Still" };

        readonly static string[] monthNames = { null, "Nachexen", "Jahrdrung", "Pflugzeit", "Sigmarzeit", "Sommerzeit", "Vorgeheim", "Nachgeheim", "Erntezeit", "Brauzeit", "Kaldezeit", "Ulriczeit", "Vorhexen" };
        readonly static string[] altMonthNames = { null, "After-Witching", "Year-Turn", "Ploughtide", "Sigmartide", "Summertide", "Fore-Mystery", "After-Mystery", "Harvest-Tide", "Brewmonth", "Chillmonth", "Ulrictide", "Fore-Witching" };


        readonly static int[] numDaysInMonth = {0, 32, 33, 33, 33, 33, 33, 32, 33, 33, 33, 33, 33 };
        readonly static int[] numDaysInMonthIncludingHolidays = { 0, 33, 33, 34, 33, 33, 34, 33, 33, 34, 33, 33, 34 };

        readonly static int numDaysInWeek = 8;

        readonly static string[] weekdayNames = {"Wellentag", "Aubentag", "Marktag", "Backertag", "Bezahltag", "Konistag", "Angestag", "Festag"};
        readonly static string[] altWeekdayNames = { "Workday", "Levyday", "Marketday", "Bakeday", "Taxday", "Kingday", "Startweek", "Holiday" };

        readonly static int mann_Cycle = 25;
        readonly static int mann_Shift = 12;
        static moonPhase[] mann_Phases;

        static moonPhase[] morr_Phases;
        static Random morrslieb;

        readonly static string[] phase_strings = { "New Moon", "Waxing Crescent", "First Quarter","Waxing Gibbous", "Full Moon", "Waning Gibbous", "Last Quarter", "Waning Crescent" };


        readonly static int startYear = 0;

        readonly static int startDay = 0;

        static string[] UniversalNoteDates = {
            "0101",
            "0300",
            "0333",
            "0418",
            "0600",
            "0633",
            "0801",
            "0900",
            "0933",
            "1200",
            "1233" };
        static string[] UniversalNoteContents = {
            "Year Blessing",
            "Holy day for Manaan, Taal, and Ulric",
            "First Quaff (Dwarfs)",
            "Sigmartag",
            "Holy day for Taal, Rhya, and Elf gods",
            "Saga (Dwarfs)",
            "Start of Pie Week (Halflings)",
            "Holy day for Rhya, Taal, and Ulric",
            "Second Breech (Dwarfs)",
            "Holy day for Ulric, Taal, an Rhya",
            "Keg End (Dwarfs)" };

        #endregion

        #region Current data
        int month;
        int day;
        int year;
        int dayOfWeek;
        int mannCounter;
        int morrCounter;
        int currentMorrSeed;
        int morrSize;
        public int MorrSize
        {
            get
            {
                return morrSize;
            }
        }
        #endregion

        #region Accessors
        public int CurrentMonth
        {
            get
            {
                return month;
            }
        }

        public int CurrentDay
        {
            get
            {
                return day;
            }
        }

        public int CurrentYear
        {
            get
            {
                return year;
            }
        }

        public static int NumMonthsInYear
        {
            get
            {
                return numMonthsInYear;
            }
        }

        public static int[] NumDaysInMonth
        {
            get
            {
                return numDaysInMonth;
            }
        }

        public static int NumDaysInWeek
        {
            get
            {
                return numDaysInWeek;
            }
        }
        #endregion


        public ImperialCalendar()
        {
            createMoonPhaseArray();
            setDate(1, 1, 2511);
        }

        // cycle / 8 = full moon day length (rounded to nearest)
        // waning crescent -> full moon takes 4 days
        private void createMoonPhaseArray()
        {
            mann_Phases = new moonPhase[mann_Cycle];

            int arrayIndex = 0; // index for adding phases to arrayToAdd


            int extraDays = mann_Cycle % 8;                         // If 8 doesn't evenly divide the cycle, there will be extra days for some phases (there are 8 phases)
            bool[] phasesWithExtraDay = extraDayPlacement(extraDays); // find out which phases might have an extra day length


            // Outer loop indexes through each phase, inner loop allocates that phase "cycle/8" times, then adds an extra day if applicable
            for (int moonPhaseIndex = 0; moonPhaseIndex < 8; moonPhaseIndex++)
            {
                for (int allocater = 0; allocater < (mann_Cycle / 8); allocater++)
                {
                    mann_Phases[arrayIndex++] = (moonPhase)moonPhaseIndex;
                }
                if (phasesWithExtraDay[moonPhaseIndex]) // add extra day
                    mann_Phases[arrayIndex++] = (moonPhase)moonPhaseIndex;
            }
            // TODO: CORRECT FOR ACTUAL CALENDAR

            morr_Phases = new moonPhase[8];
            currentMorrSeed = 0;
            for (int moonPhaseIndex = 0; moonPhaseIndex < 8; moonPhaseIndex++)
            {
                morr_Phases[moonPhaseIndex] = (moonPhase)moonPhaseIndex;
            }

        }

        /// <summary>
        /// Returns which moon phases get extra days, depending on how many extra days there are
        /// Extra days range from 0 - 7 (modulo 8, since there's 8 phases), 0 extra days = all phases same length
        /// </summary>
        /// <param name="extraDays"></param>
        private bool[] extraDayPlacement(int extraDays)
        {
            // CHECK
            //  moonPhase {newMoon, waxingCrsec, firstQuarter, waxingGib, full, waningGib, lastQuarter, waningCresc};
            // Would like to do this elegantly, with simple calculation, but this is based off data from donjon, so not sure how it exactly works
            switch (extraDays)
            {
                case 0:
                    return new bool[] { false, false, false, false,  false, false, false, false};
                case 1:
                    return new bool[] {true, false, false, false, false, false, false, false};
                case 2:
                    return new bool[] {true, false, false, false, true, false, false, false };
                case 3:
                    return new bool[] {true, false, true, false, false, true, false, false };
                case 4:
                    return new bool[] { true, false, true, false, true, false, true, false };
                case 5:
                    return new bool[] {true, true, false, true, true, false, true, false };
                case 6:
                    return new bool[] {true, true, true, false, true, true, true, false };
                case 7:
                    return new bool[] { true, true, true, true, true, true, true, false};
                default:
                    return null;
            }

        }

        /// <summary>
        /// If the moon's cycle is less than 8 days, all 8 phases won't be used, this function decides which ones to use given a cycle
        /// </summary>
        /// <param name="moonCycle"></param>
        /// <returns></returns>
        private bool[] removedPhases(int moonCycle)
        {
            // CHECK
            switch (moonCycle)
            {
                case 7:
                    return new bool[] {true, true, true, true, true, true, true, false };
                case 6:
                    return new bool[] {true, true, true, false, true, true, true, false};
                case 5:
                    return new bool[] {false, true, true, false, true, true, false, true};
                case 4:
                    return new bool[] { true, false, true, false, true, false, true, false };
                case 3:
                    return new bool[] {true, false, true, false, false, true, false, false };
                case 2:
                    return new bool[] { true, false, false, false, true, false, false, false };
                case 1:
                    return new bool[] { true, false, false, false, false, false, false, false };
                case 0:
                    return null;
                default:
                    return new bool[] { true, true, true, true, true, true, true, true };
            }
        }

        #region forward in time
        public void addDay()
        {
            day++;
            if (day > numDaysInMonth[month])
            {
                day = 1;
                month++;
                if (month > numMonthsInYear)
                {
                    day = 0; // Hexentag
                    month = 1;
                    year++;
                    subDayOfWeek(); // Intercalary days are not considered a weekday, cancel out the addDayofWeek() below
                                    // Jank implementation but whatever
                }
                else if (month == 3 || month == 6 || month == 7 || month == 9 || month == 12)
                {
                    day = 0;
                    subDayOfWeek();// Intercalary days are not considered a weekday
                                   // Jank implementation but whatever
                }
            }
            addDayOfWeek();
            addMoonPhase();

            if (year > 9999)
            {
                year = 9999;
                month = numMonthsInYear;
                day = numDaysInMonth[numMonthsInYear];
                // Kinda jank, if this if statement happens, the date doesnt change, so reverse the adddayofweek and addmoonphase
                subDayOfWeek();
                subMoonPhase();
            }
        }
        public void addDay(int numDays)
        {
            for (int i = 0; i < numDays; i++)
                addDay();
        }
        public void addWeek()
        {
            addDay(numDaysInWeek);
        }
        public void addMonth()
        {
            addDay(numDaysInMonth[month]);
        }
        public void addYear()
        {
            year++;
            if (year > 9999)
                year = 9999;
            determineDayOfWeek();
            determineMoonCounters();
        }
        public void addDayOfWeek()
        {
            dayOfWeek++;
            if (dayOfWeek >= numDaysInWeek - 1) // >= because dayOfWeek goes from 0 to numDaysInWeek - 1
                dayOfWeek = 0;
        }

        public void addMoonPhase()
        {
            mannCounter++;
            if (mannCounter >= mann_Cycle)
                mannCounter = 0;

            morrsliebNextPhase();

        }
        #endregion

        #region backward in time
        public void subDay()
        {
            day--;
            if (day < 1)
            {
                if (month == 1 || month == 3 || month == 6 || month == 7 || month == 9 || month == 12)
                {
                    if (day < 0)
                    {
                        month--;
                        if (month < 1)
                        {
                            year--;
                            month = numMonthsInYear;
                            day = numDaysInMonth[month];
                        }
                        day = numDaysInMonth[month];
                    }
                    else // if day == 0 and is intercalary day
                        addDayOfWeek(); // jank but whatever, offset subdayofweek(), intercalary days are not considered a weekday

                }
                else
                {
                    month--;
                    if (month < 1)
                    {
                        year--;
                        month = numMonthsInYear;
                        day = numDaysInMonth[month];
                    }
                    day = numDaysInMonth[month];
                }


            }
            subDayOfWeek();
            subMoonPhase();

            if (year < 0)
            {
                day = 0;
                month = 1;
                year = 0;
                addDayOfWeek();
                addMoonPhase();
            }
        }
        public void subDay(int numDays)
        {
            for (int i = 0; i < numDays; i++)
            {
                subDay();
            }
        }
        public void subWeek()
        {
            subDay(numDaysInWeek);
        }
        public void subMonth()
        {
            subDay(numDaysInMonth[month]);
        }
        public void subYear()
        {
            year--;
            if (year < 0)
                year = 0;
            determineDayOfWeek();
            determineMoonCounters();
        }
        public void subDayOfWeek()
        {
            dayOfWeek--;
            if (dayOfWeek < 0)
                dayOfWeek = numDaysInWeek - 1;
        }

        public void subMoonPhase()
        {
            mannCounter--;
            if (mannCounter < 0)
                mannCounter = mann_Cycle - 1;

            determineMorrslieb();

        }
        #endregion


        #region setDate and all functions determining day of week, moonphase, etc

        public void setDate(string dateString)
        {
            setDate(Int32.Parse(dateString.Substring(0, 2)), Int32.Parse(dateString.Substring(2, 2)), Int32.Parse(dateString.Substring(4, 4)));
        }

        public void setDate(int m, int d, int y)
        {
            if (m > numMonthsInYear)
                m = numMonthsInYear;
            else if (m <= 0)
                m = 1;

            month = m;

            if (d > numDaysInMonth[month])
                d = numDaysInMonth[month];

            else if (d < 1)
            {
                if (month == 1 || month == 3 || month == 6 || month == 7 || month == 9 || month == 12)
                {
                    day = 0;
                }
                else
                {
                    day = 1;
                }
            }
            else
                day = d;

            if (y < 0)
                y = 1;
            else if (y > 9999)
                year = 9999;

            year = y;

            dayOfWeek = determineDayOfWeek();
            determineMoonCounters();
        }

        public int determineDayOfWeek()
        {
            return determineDayOfWeek(month, day, year);
        }

        public static int determineDayOfWeek(string currentDate)
        {
            return determineDayOfWeek(Int32.Parse(currentDate.Substring(0, 2)), Int32.Parse(currentDate.Substring(2, 2)), Int32.Parse(currentDate.Substring(4, 4)));
        }

        public static int determineDayOfWeek(int m, int d, int y)
        {
            int yearDifference = Math.Abs(y - startYear);
            int daysToSubtract = yearDifference * 6;

            // Since intercalary days are not considered weekdays, we have to subtract however many have passed since the start date and the input date
            switch (m)
            {
                case 1:
                    if (d > 0)
                        daysToSubtract++;
                    break;
                case 2:
                    daysToSubtract++;
                    break;
                case 3:
                    if (d > 0)
                        daysToSubtract += 2;
                    else
                        daysToSubtract++;
                    break;
                case 4:
                    daysToSubtract += 2;
                    break;
                case 5:
                    daysToSubtract += 2;

                    break;
                case 6:
                    if (d > 0)
                        daysToSubtract += 3;
                    else
                        daysToSubtract += 2;
                    break;
                case 7:
                    if (d > 0)
                        daysToSubtract += 4;
                    else
                        daysToSubtract += 3;
                    break;
                case 8:
                    daysToSubtract += 4;
                    break;
                case 9:
                    if (d > 0)
                        daysToSubtract += 5;
                    else
                        daysToSubtract += 4;
                    break;
                case 10:
                    daysToSubtract += 5;
                    break;
                case 11:
                    daysToSubtract += 5;
                    break;
                case 12:
                    if (d > 0)
                        daysToSubtract += 6;
                    else
                        daysToSubtract += 5;
                    break;
            }

            int totalDaysPassedSinceStart = yearDifference * numDaysInYear + (determineDayOfYear(m, d, y) - 1);
            int modResult = (totalDaysPassedSinceStart - daysToSubtract) % (numDaysInWeek);
            return ((modResult + startDay) % (numDaysInWeek));
        }

        /// <summary>
        /// For each moon, use its cycle and shift to determine the current phase
        /// This is calculated from the calendar's year 0 day 1 (which is what the cycle and shift are relative to)
        /// </summary>
        public void determineMoonCounters()
        {
            int daysSinceFirstDay = 0;

            for (int i = 0; i < Math.Abs(year); i++)
                daysSinceFirstDay += numDaysInYear;

            daysSinceFirstDay += determineDayOfYear() - 1;

              mannCounter = Math.Abs(daysSinceFirstDay - mann_Shift) % mann_Cycle;

            // For some reason, if the year is zero, the phases are off by one, not sure why
            // Has to do with determineDayofYear adding 1 if month starts at 0, but that's necessary
            if (year == 0)
                addMoonPhase();

            determineMorrslieb();

        }

        /// <summary>
        /// Determines the next morrslieb phase
        /// Calls a new Random.Next() for every day
        /// </summary>
        private void morrsliebNextPhase()
        {
            // The operations used to determine morrslieb have to be repeated in full when REdetermining morrslieb.
            // When determining the next phase of morr, it can't be full at the same time as mann (on a normal day)
            // so a while loop is used to keep generating until morr isn't full. This must be also done when redetermining
            // to ensure the same result is received
            if (year != currentMorrSeed)
            {
                morrslieb = new Random(year);
                currentMorrSeed = year;
                int days = determineDayOfYear();
                for (int i = 0; i < days - 1; i++)
                {
                    morrSize = morrslieb.Next(50, 200);
                    morrCounter = morrSize % 8;
                    while (mann_Phases[mannCounter] == moonPhase.full && morr_Phases[morrCounter] == moonPhase.full) // <- this must be done even when it's not used (redetermining)
                    {
                        morrSize = morrslieb.Next(50, 200);
                        morrCounter = morrSize % 8;
                    }
                }

            }
            morrSize = morrslieb.Next(50, 200);
            morrCounter = morrSize % 8;

            while (mann_Phases[mannCounter] == moonPhase.full && morr_Phases[morrCounter] == moonPhase.full)
            {
                morrSize = morrslieb.Next(50, 200);
                morrCounter = morrSize % 8;
            }

            // If it is geheimnistag or hexentag
            if ((day == 0 && (month == 1 || month == 7)))
            {
                morrSize = 200;
                morrCounter = 4;
            }
        }

        /// <summary>
        /// Since morrsliebNextPhase calls Random.Next() for every day, going backwards doesn't work (a new Next() would not be the same as the previous)
        /// Therefore, have to redo, call Next() for every day so far this year, stop at the current day
        /// </summary>
        private void determineMorrslieb()
        {
            currentMorrSeed = -1;
            morrsliebNextPhase();
        }

        public string determineCurrentStarSign()
        {
            return determineStarSignFromDate(month, day);

        }

        public static string determineStarSignFromDate(int m, int d)
        {
            switch (StarSignNumber(m, d))
            {
                case 1:
                    return "Wymund the Anchorite";
                case 2:
                    return "The Big Cross";
                case 3:
                    return "The Limner's Line";
                case 4:
                    return "Gnuthus the Ox";
                case 5:
                    return "Dragomas the Drake";
                case 6:
                    return "The Gloaming";
                case 7:
                    return "Grungi's Baldrick";
                case 8:
                    return "Mammit the Wise";
                case 9:
                    return "Mummit the Fool";
                case 10:
                    return "The Two Bullocks";
                case 11:
                    return "The Dancer";
                case 12:
                    return "The Drummer";
                case 13:
                    return "The Piper";
                case 14:
                    return "Vobist the Faint";
                case 15:
                    return "The Broken Cart";
                case 16:
                    return "The Greased Goat";
                case 17:
                    return "Rhya's Cauldron";
                case 18:
                    return "Cacklefax the Cockerel";
                case 19:
                    return "The Bonesaw";
                case 20:
                    return "The Witchling Star";

            }
            return null;
        }

        public static int StarSignNumber(int m, int d)
        {
            switch (m)
            {
                case 1:
                    if (d <= 7)
                        return 1;
                    else if (d <= 27)
                        return 2;
                    else
                        return 3;
                case 2:
                    if (d <= 15)
                        return 3;
                    else
                        return 4;
                case 3:
                    if (d <= 1)
                        return 4;
                    else if (d <= 21)
                        return 5;
                    else
                        return 6;
                case 4:
                    if (d <= 8)
                        return 6;
                    else if (d <= 28)
                        return 7;
                    else
                        return 8;
                case 5:
                    if (d <= 15)
                        return 8;
                    else
                        return 9;
                case 6:
                    if (d <= 1)
                        return 9;
                    else if (d <= 21)
                        return 10;
                    else
                        return 11;
                case 7:
                    if (d <= 7)
                        return 11;
                    else if (d <= 27)
                        return 12;
                    else
                        return 13;
                case 8:
                    if (d <= 15)
                        return 13;
                    else
                        return 14;
                case 9:
                    if (d <= 1)
                        return 14;
                    else if (d <= 21)
                        return 15;
                    else
                        return 16;
                case 10:
                    if (d <= 8)
                        return 16;
                    else if (d <= 28)
                        return 17;
                    else
                        return 18;
                case 11:
                    if (d <= 15)
                        return 18;
                    else
                        return 19;
                case 12:
                    if (d <= 1)
                        return 19;
                    else if (d <= 21)
                        return 20;
                    else
                        return 1;

            }
            return 0;
        }

        #endregion


        #region determineDayOfYear
        public int determineDayOfYear()
        {
            return determineDayOfYear(month, day, year);
        }

        public static int determineDayOfYear(string currentDate)
        {
            return determineDayOfYear(Int32.Parse(currentDate.Substring(0, 2)), Int32.Parse(currentDate.Substring(2, 2)), Int32.Parse(currentDate.Substring(4, 4)));
        }

        public static int determineDayOfYear(int m, int d, int y)
        {
            int dayAccumulator = 0;

            // Add the days of the month before current month
            for (int i = 0; i < m - 1; i++)
            {
                dayAccumulator += numDaysInMonthIncludingHolidays[i + 1];
            }
            dayAccumulator += d; // add current day to sum
            if (isMonthWithHoliday(m))
                dayAccumulator++;
            return dayAccumulator;
        }

#endregion

        #region returning date, moonphases, names, etc.

        public string getMonthName()
        {
            return monthNames[month];
        }

        public string returnMoonNames()
        {
            StringBuilder sb = new StringBuilder();

            return sb.ToString();
        }

        public string[] currentMoonPhases()
        {
            return new string[] { phase_strings[(int)(mann_Phases[mannCounter])], phase_strings[(int)(morr_Phases[morrCounter])]};
        }


        //TODO: NOT STABLE
        /// <summary>
        /// Reverse ReturnGivenDate.
        /// Give (monthName) (dayNumber) (yearNumber)
        /// Returns mmddyyyy
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string ReturnGivenDateFromName(string date)
        {
            string month = null, day = null, year = null;
            try
            {
                string[] splitArray = date.Split(' ');
                for (int i = 0; i < splitArray.Length; i++)
                {
                    splitArray[i] = splitArray[i].Trim(',');
                }
                // If the length is 2, intercalary holiday
                if (splitArray.Length == 2)
                {
                    for (int i = 1; i <= numMonthsInYear; i++)
                    {
                        if (intercalaryHolidays[i] == splitArray[0])
                        {
                            month = i.ToString("00");
                            year = splitArray[1];
                            day = "00";
                        }
                    }
                }

                if (splitArray.Length == 3)
                {
                    for (int i = 1; i < monthNames.Length; i++)
                    {
                        if (splitArray[0].Equals(monthNames[i]))
                        {
                            month = enforceMonthFormat(i.ToString());
                        }

                    }
                    year = enforceYearFormat(splitArray[2]);
                    day = enforceDayFormat(month, splitArray[1], year);
                }
            }
            catch (Exception e)
            {

            }
            return month + day + year;
        }

        public string ReturnUniversalNoteContent()
        {
            for (int i = 0; i < UniversalNoteContents.Length; i++)
            {
                if (isAnniversary(UniversalNoteDates[i] + "0000"))
                    return UniversalNoteContents[i];
            }
            return null;
        }
        #endregion


        #region Date relation functions, sameDate, isAnniversary, yearsAgo, farthestInTime

        public bool sameDate(int testM, int testD, int testY)
        {
            if (testM == month && testD == day && testY == year)
                return true;
            else
                return false;
        }

        public bool sameDate(string testDate)
        {
            if (testDate.Length != 8)
                return false;
            else
                return sameDate(Int32.Parse(testDate.Substring(0, 2)), Int32.Parse(testDate.Substring(2, 2)), Int32.Parse(testDate.Substring(4, 4)));
        }

        public bool isAnniversary(string testDate)
        {
            if (testDate.Length != 8)
                return false;
            else
                return isAnniversary(Int32.Parse(testDate.Substring(0, 2)), Int32.Parse(testDate.Substring(2, 2)));
        }

        public bool isAnniversary(int testM, int testD)
        {
            if (testM == month && testD == day)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Difference between input year and current year
        /// </summary>
        /// <param name="y">Input year</param>
        /// <returns>Difference between current year and y</returns>
        public int yearsAgo(int y)
        {
            return year - y;
        }

        /// <summary>
        /// Difference in years between input date and current date
        /// </summary>
        /// <param name="inputDate">Input date in the form of MMDDYYYY</param>
        /// <returns></returns>
        public int yearsAgo(string inputDate)
        {
            return yearsAgo(this.ToString(), inputDate);
        }

        public static int yearsAgo(string initialDate, string compareDate)
        {
            return Int32.Parse(initialDate.Substring(4, 4)) - Int32.Parse(compareDate.Substring(4, 4));
        }

        // returns true if this month begins with an intercalary day (month starts at day 0)
        public static bool isMonthWithHoliday(int m)
        {
            return (m == 1 || m == 3 || m == 6 || m == 7 || m == 9 || m == 12);

        }

        public static int FarthestInTime(string date1, string date2)
        {
            int year1 = Int32.Parse(date1.Substring(4, 4));
            int year2 = Int32.Parse(date2.Substring(4, 4));
            int month1 = Int32.Parse(date1.Substring(0, 2));
            int month2 = Int32.Parse(date2.Substring(0, 2));
            int day1 = Int32.Parse(date1.Substring(2, 2));
            int day2 = Int32.Parse(date2.Substring(2, 2));

            if (year1 > year2)
                return 1;
            else if (year2 > year1)
                return -1;
            else // if year1 == year2
            {
                if (month1 > month2)
                    return 1;
                else if (month2 > month1)
                    return -1;
                else // if month1 == month2
                {
                    if (day1 > day2)
                        return 1;
                    else if (day2 > day1)
                        return -1;
                    else // day1 == day 2
                        return 0;
                }
            }
        }

        /// <summary>
        /// Returns true if testDate is between date1 and date2
        /// </summary>
        /// <param name="testDate"></param>
        /// <param name="date1"></param>
        /// <param name="date2"></param>
        /// <returns></returns>
        //public static bool dateBetween(string testDate, string date1, string date2)
        //{
        //}

        #endregion

        /// <summary>
        /// Formats current date as MMDDYYYY
        /// </summary>
        /// <returns>Returns current date as a string in the format of MMDDYYYY</returns>
        public override string ToString()
        {
            StringBuilder stringDate = new StringBuilder();

            stringDate.Append(month.ToString("00"));
            stringDate.Append(day.ToString("00"));
            stringDate.Append(year.ToString("0000"));

            return stringDate.ToString();
        }

        public string ToString(string format, bool alt = false)
        {
            return ToString(ToString(), format, alt);
        }

        public static string ToString(string dateString, string format, bool alt = false)
        {
            int m = Int32.Parse(dateString.Substring(0, 2));
            int d = Int32.Parse(dateString.Substring(2, 2));
            int y = Int32.Parse(dateString.Substring(4, 4));

            format = format.ToLower();



            /// First Column    Second Column
            /// Index Marker |  Marker Length
            /// D4
            /// D
            /// M
            /// Y
            int[,] offLimitArray = new int[4, 2];
            for (int i = 0; i < offLimitArray.GetLength(0); i++)
            {
                for (int j = 0; j < offLimitArray.GetLength(1); j++)
                {
                    offLimitArray[i, j] = 0;
                }
            }


            int startIndex = 0;

            if (format.Contains("dddd"))
            {
                offLimitArray[0, 0] = format.IndexOf("dddd");
                offLimitArray[0, 1] = ReturnDayFromFormat("dddd", dateString).Length;
                format = format.Replace("dddd", ReturnDayFromFormat("dddd", dateString));
            }

            while (startIndex < format.Length)
            {
                if (format.IndexOf("ddd", startIndex) != -1 && !OffLimits(startIndex = format.IndexOf("ddd", startIndex), offLimitArray))
                {
                    offLimitArray[1, 0] = format.IndexOf("ddd");
                    offLimitArray[1, 1] = ReturnMonthFromFormat("ddd", m).Length;
                    int substringStart = format.IndexOf("ddd", startIndex);
                    string substring = format.Substring(startIndex);
                    format = format.Substring(0, substringStart) + substring.Replace("ddd", ReturnDayFromFormat("ddd", dateString));
                }
                else if (format.IndexOf("dd", startIndex) != -1 && !OffLimits(startIndex = format.IndexOf("dd", startIndex), offLimitArray))
                {
                    offLimitArray[1, 0] = format.IndexOf("dd");
                    offLimitArray[1, 1] = ReturnMonthFromFormat("dd", m).Length;
                    int substringStart = format.IndexOf("dd", startIndex);
                    string substring = format.Substring(startIndex);
                    format = format.Substring(0, substringStart) + substring.Replace("dd", ReturnDayFromFormat("dd", dateString));
                }
                else if (format.IndexOf("d", startIndex) != -1 && !OffLimits(startIndex = format.IndexOf("d", startIndex), offLimitArray))
                {
                    offLimitArray[1, 0] = format.IndexOf("d");
                    offLimitArray[1, 1] = ReturnMonthFromFormat("d", m).Length;
                    int substringStart = format.IndexOf("d", startIndex);
                    string substring = format.Substring(startIndex);
                    format = format.Substring(0, substringStart) + substring.Replace("d", ReturnDayFromFormat("d", dateString));
                }
                else
                    startIndex++;
            }


            startIndex = 0;
            while (startIndex < format.Length)
            {
                if (format.Contains("mmm"))
                {
                    if (IsIntercalAt(dateString) != 0)
                    {
                        bool yearPresent = format.Contains("y");

                        if (yearPresent)
                            return IntercalHolidayAt(dateString) + ", " + y.ToString("0000");
                        else
                            return IntercalHolidayAt(dateString);
                    }
                    else if (format.IndexOf("mmm", startIndex) != -1 && !OffLimits(startIndex = format.IndexOf("mmm", startIndex), offLimitArray))
                    {
                        offLimitArray[2, 0] = format.IndexOf("mmm");
                        offLimitArray[2, 1] = ReturnMonthFromFormat("mmm", m).Length;
                        int substringStart = format.IndexOf("mmm", startIndex);
                        string substring = format.Substring(startIndex);
                        format = format.Substring(0, substringStart) + substring.Replace("mmm", ReturnMonthFromFormat("mmm", m));
                    }
                }
                else if (format.IndexOf("mm", startIndex) != -1 && !OffLimits(startIndex = format.IndexOf("mm", startIndex), offLimitArray))
                {
                    offLimitArray[2, 0] = format.IndexOf("mm");
                    offLimitArray[2, 1] = ReturnMonthFromFormat("mm", m).Length;
                    int substringStart = format.IndexOf("mm", startIndex);
                    string substring = format.Substring(startIndex);
                    format = format.Substring(0, substringStart) + substring.Replace("mm", ReturnMonthFromFormat("mm", m));
                }
                else if (format.IndexOf("m", startIndex) != -1 && !OffLimits(startIndex = format.IndexOf("m", startIndex), offLimitArray))
                {
                    offLimitArray[2, 0] = format.IndexOf("m");
                    offLimitArray[2, 1] = ReturnMonthFromFormat("m", m).Length;
                    int substringStart = format.IndexOf("m", startIndex);
                    string substring = format.Substring(startIndex);
                    format = format.Substring(0, substringStart) + substring.Replace("m", ReturnMonthFromFormat("m", m));
                }
                else
                    startIndex++;
            }

            startIndex = 0;
            while (startIndex < format.Length)
            {

                if (format.IndexOf("yyyy", startIndex) != -1 && !OffLimits(startIndex = format.IndexOf("yyyy", startIndex), offLimitArray))
                {
                    offLimitArray[3, 0] = format.IndexOf("yyyy", startIndex);
                    offLimitArray[3, 1] = ReturnYearFromFormat("yyyy", y).Length;
                    int substringStart = format.IndexOf("yyyy", startIndex);
                    string substring = format.Substring(startIndex);
                    format = format.Substring(0, substringStart) + substring.Replace("yyyy", ReturnYearFromFormat("yyyy", y));
                }
                else if (format.IndexOf("yyy", startIndex) != -1 && !OffLimits(startIndex = format.IndexOf("yyy", startIndex), offLimitArray))
                {
                    offLimitArray[3, 0] = format.IndexOf("yyy", startIndex);
                    offLimitArray[3, 1] = ReturnYearFromFormat("yyy", y).Length;
                    int substringStart = format.IndexOf("yyy", startIndex);
                    string substring = format.Substring(startIndex);
                    format = format.Substring(0, substringStart) + substring.Replace("yyy", ReturnYearFromFormat("yyy", y));
                }
                else if (format.IndexOf("yy", startIndex) != -1 && !OffLimits(startIndex = format.IndexOf("yy", startIndex), offLimitArray))
                {
                    offLimitArray[3, 0] = format.IndexOf("yy", startIndex);
                    offLimitArray[3, 1] = ReturnYearFromFormat("yy", y).Length;
                    int substringStart = format.IndexOf("yy", startIndex);
                    string substring = format.Substring(startIndex);
                    format = format.Substring(0, substringStart) + substring.Replace("yy", ReturnYearFromFormat("yy", y));
                }
                else if (format.IndexOf("y", startIndex) != -1 && !OffLimits(startIndex = format.IndexOf("y", startIndex), offLimitArray))
                {
                    offLimitArray[3, 0] = format.IndexOf("y", startIndex);
                    offLimitArray[3, 1] = ReturnYearFromFormat("y", y).Length;
                    int substringStart = format.IndexOf("y", startIndex);
                    string substring = format.Substring(startIndex);
                    format = format.Substring(0, substringStart) + substring.Replace("y", ReturnYearFromFormat("y", y));
                }
                else
                    startIndex++;
            }
            return format;
        }

        private static bool OffLimits(int indexFound, int[,] offLimitData)
        {
            for (int i = 0; i < offLimitData.GetLength(0); i++)
            {
                // If index is greater than the start index and less than the start index + length of marker
                // (if in the offlimit zone)
                if (indexFound > offLimitData[i, 0] && indexFound < offLimitData[i, 0] + offLimitData[i, 1])
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Input format as...
        /// dddd -> returns day of week
        /// ddd  -> "(day)st/nd/rd/th"
        /// dd   -> "01" to "31"
        /// none ->  "1" to "31"
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        private static string ReturnDayFromFormat(string format, string dateString)
        {
            int dayValue = Int32.Parse(dateString.Substring(2, 2));
            string returnString;
            switch (format)
            {
                case "dddd":
                    returnString = weekdayNames[determineDayOfWeek(dateString)];
                    break;
                case "ddd":
                    if (dayValue == 1)
                        returnString = dayValue + "st";
                    else if (dayValue == 2)
                        returnString = dayValue + "nd";
                    else if (dayValue == 3)
                        returnString = dayValue + "rd";
                    else
                        returnString = dayValue + "th";
                    break;
                case "dd":
                    returnString = dayValue.ToString("00");
                    break;

                case "d":
                    returnString = dayValue.ToString();
                    break;
                default:
                    returnString = dayValue.ToString();
                    break;
            }
            return returnString;
        }

        /// <summary>
        /// Input format as...
        /// mmm -> "(month name)" or if intercalary holiday, return holiday name
        /// mm  -> "01" to "12"
        /// m   ->  "1" to "12" 
        /// none -> "1" to "12"
        /// </summary>
        /// <param name="format"></param>
        /// <param name="alt">if you want alternative month names</param>
        /// <returns></returns>
        private static string ReturnMonthFromFormat(string format, int monthValue, bool alt = false)
        {
            string returnMonth;
            switch (format)
            {
                case "mmm":
                    if (alt)
                        returnMonth = altMonthNames[monthValue];
                    else
                        returnMonth = monthNames[monthValue];
                    break;

                case "mm":
                    returnMonth = monthValue.ToString("00");
                    break;

                case "m":
                    returnMonth = monthValue.ToString();
                    break;
                default:
                    returnMonth = monthValue.ToString();
                    break;
            }
            return returnMonth;
        }

        private string ReturnMonthFromFormat(string format, bool alt = false)
        {
            return ReturnMonthFromFormat(format, month, alt);
        }

        /// <summary>
        /// Input format as...
        /// yyyy -> "0000" to "9999"
        /// yyy ->   "000" to  "999" right most 3 numbers, probably useless
        /// yy ->     "00" to   "99" right most 2 numbers, probably useless
        /// y ->       "0" to    "9" right most number, probably useless
        /// none ->    "0  to "9999"
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        private static string ReturnYearFromFormat(string format, int yearValue)
        {
            string returnYear;

            switch (format)
            {
                case "yyyy":
                    returnYear = yearValue.ToString("0000");
                    break;
                case "yyy":
                    returnYear = yearValue.ToString("000"); // TEST
                    break;
                case "yy":
                    returnYear = yearValue.ToString("00");
                    break;
                case "y":
                    returnYear = yearValue.ToString("0");
                    break;
                default:
                    returnYear = yearValue.ToString();
                    break;
            }
            return returnYear;
        }

        private string ReturnYearFromFormat(string format)
        {
            return ReturnYearFromFormat(format, year);
        }

        #region Holiday Determination

        public string CurrentHolidayName()
        {
            return IntercalHolidayAt(month, day, year);
        }


        public bool IsHoliday()
        {
            if (IsIntercalAt(month, day, year) != 0)
            {
                return true;
            }
            else
                return false;

        }

        public static string IntercalHolidayAt(string dateString)
        {
            string m = dateString.Substring(0, 2);
            string d = dateString.Substring(2, 2);
            string y = dateString.Substring(4, 4);

            enforceDateFormat(ref m, ref d, ref y);
            return IntercalHolidayAt(Int32.Parse(m), Int32.Parse(d), Int32.Parse(y));

        }

        public static string IntercalHolidayAt(int m, int d, int y)
        {
            int holidayStatus = IsIntercalAt(m, d, y);

            if (holidayStatus != 0)
            {
                return intercalaryHolidays[holidayStatus];
            }
            else
            {
                return null;
            }
        }


        public static int IsIntercalAt(string dateString)
        {
            string m = dateString.Substring(0, 2);
            string d = dateString.Substring(2, 2);
            string y = dateString.Substring(4, 4);

            enforceDateFormat(ref m, ref d, ref y);
            return IsIntercalAt(Int32.Parse(m), Int32.Parse(d), Int32.Parse(y));
        }


        public static int IsIntercalAt(int m, int d, int y)
        {
            if (d == 0 && (m == 1 || m == 3 || m == 6 || m == 7 || m == 9 || m == 12))
            {
                return m;
            }
            else
                return 0;
        }

        #endregion

        #region functions for format enforcement

        /// <summary>
        /// Performs all enforce functions on an entire date
        /// </summary>
        /// <param name="testMonth"></param>
        /// <param name="testDay"></param>
        /// <param name="testYear"></param>
        public static void enforceDateFormat(ref string testMonth, ref string testDay, ref string testYear)
        {
            testMonth = enforceMonthFormat(testMonth);
            testYear = enforceYearFormat(testYear);
            testDay = enforceDayFormat(testDay, testDay, testYear);
        }


        /// <summary>
        /// Takes a number and changes it to a valid month number if it is not already one
        /// (if a number is larger than 12, returns 12, for example)
        /// </summary>
        /// <param name="testMonth">input number to test as a month</param>
        /// <returns>returns a correct month number</returns>
        public static string enforceMonthFormat(string testMonth)
        {
            if (testMonth.Length < 2)
            {
                if (testMonth.Length == 0)
                    testMonth = "0" + testMonth;
                testMonth = "0" + testMonth;
            }

            if (testMonth == "00" || testMonth == "")
            {
                testMonth = "01";
            }

            if (testMonth.Length > 2 || Int32.Parse(testMonth) > numMonthsInYear)
            {
                testMonth = numMonthsInYear.ToString();
            }
            return testMonth;
        }

        /// <summary>
        /// Returns a valid day value, depending on what year and month
        /// </summary>
        /// <param name="month">month value used to determine valid day (some months have 31 days)</param>
        /// <param name="testDay">day value, being tested</param>
        /// <param name="year">year value used to determine valid day (possible leap year)</param>
        /// <returns>returns a valid day value corresponding with the month and year</returns>
        public static string enforceDayFormat(string month, string testDay, string year)
        {
            if (testDay.Length < 2)
            {
                if (testDay.Length == 0)
                    testDay = "0" + testDay;
                testDay = "0" + testDay;
            }

            if (testDay == "")
            {
                testDay = "01";
            }
            if (month != "")
            {
                testDay = verifyDay(Int32.Parse(month), Int32.Parse(testDay)).ToString("00");
            }
            else
            {
                testDay = "01";
            }

            return testDay;
        }

        /// <summary>
        /// Returns year in valid format, input of 0 returns 0000, 1 returns 0001, etc.
        /// </summary>
        /// <param name="testYear">year being tested</param>
        /// <returns>formatted year</returns>
        public static string enforceYearFormat(string testYear)
        {
            if (testYear.Length > 4)
                testYear = testYear.Substring(0, 4);

            if (testYear.Length == 3)
                testYear = "0" + testYear;

            if (testYear.Length == 2)
                testYear = "00" + testYear;

            if (testYear.Length == 1)
                testYear = "000" + testYear;

            if (testYear == "")
                testYear = "0000";

            return testYear;
        }

        /// <summary>
        /// Verifies that given day is possible in given month
        /// includes day = 0 for intercalary holidays
        /// </summary>
        /// <param name="m"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static int verifyDay(int m, int d)
        {
            if (m > numMonthsInYear || m < 1) // not what month is so just return d
                return d;
            if (d > 0 && d <= numDaysInMonth[m]) // if between 1 and numdays in month
                return d;
            if (d <= 0 && (m == 1 || m == 3 || m == 6 || m == 7 || m == 9 || m == 12)) // If day == 0 possible intercalary holiday, check for that
                return 0;
            else if (d <= 0)                                                           // If not, return 1
                return 1;
            else
                return numDaysInMonth[m];
        }

        public static int verifyDay(string date)
        {
            return verifyDay(Int32.Parse(date.Substring(0, 2)), Int32.Parse(date.Substring(2, 2)));
        }
        #endregion

        #region 'dateIn' and 'daysTo' and 'daysBetween' functions

        /// <summary>
        /// Calculates the date after numDays days
        /// </summary>
        /// <param name="numDays">number of days</param>
        /// <returns></returns>
        public string dateIn(int numDays)
        {
            return dateIn(month, day, year, numDays);
        }

        /// <summary>
        /// Finds the date it will be from startDate after (numDays) days pass
        /// </summary>
        /// <param name="startDate">Starting date, MMDDYYYY </param>
        /// <param name="numDays">Number of days that pass</param>
        /// <returns></returns>
        public static string dateIn(string startDate, int numDays)
        {
            return dateIn(Int32.Parse(startDate.Substring(0, 2)), Int32.Parse(startDate.Substring(2, 2)), Int32.Parse(startDate.Substring(4, 4)), numDays);
        }

        /// <summary>
        /// Finds the date it will be from startDate after (numDays) days pass 
        /// </summary>
        /// <param name="startMonth">Starting date's month</param>
        /// <param name="startDay">Starting date's day</param>
        /// <param name="startYear">starting date's year</param>
        /// <param name="numDays">Number of days that pass</param>
        /// <returns></returns>
        public static string dateIn(int startMonth, int startDay, int startYear, int numDays)
        {
            int m = startMonth;
            int d = startDay;
            int y = startYear;


            if (d == 0 && numDays > 0)
            {
                d++;
                numDays--;
            }

            while (d + numDays > numDaysInMonth[m])
            {
                if (d == 0)
                {
                    numDays -= numDaysInMonthIncludingHolidays[m];
                }
                else
                {
                    numDays -= numDaysInMonth[m] - d;
                    d = 0;
                }

                m++;
                if (m > numMonthsInYear)
                {
                    m = 1;
                    y++;
                }

            }

            d += numDays;
            if (isMonthWithHoliday(m) && (m != startMonth || y != startYear))
                d--;

            //d = numDays == 0 ? numDaysInMonth[startMonth] - d : numDays;


            string monthString = enforceMonthFormat(m.ToString());
            string yearString = enforceYearFormat(y.ToString());
            string dayString = enforceDayFormat(monthString, d.ToString(), yearString);
            return monthString + dayString + yearString;
        }


        // TODO: if 100 days from vorhexen 33, becomes 99, if 99 from vorhexen 33, becomes 67

        /// <summary>
        /// returns the amount of days between current date and input date
        /// </summary>
        /// <param name="toMonth"></param>
        /// <param name="toDay"></param>
        /// <param name="toYear"></param>
        /// <returns></returns>
        public int daysTo(int toMonth, int toDay, int toYear)
        {
            return daysBetween(month, day, year, toMonth, toDay, toYear);
        }

        /// <summary>
        /// Calculates how many days there are between current date and input date
        /// </summary>
        /// <param name="dateString">Input date formatted as MMDDYYYY</param>
        /// <returns></returns>
        public int daysTo(string dateString)
        {
            return daysTo(Int32.Parse(dateString.Substring(0, 2)), Int32.Parse(dateString.Substring(2, 2)), Int32.Parse(dateString.Substring(4, 4)));
        }

        /// <summary>
        /// Calculates how many days between the 'begin' date and 'to' date
        /// </summary>
        /// <param name="beginMonth">beginning date's month</param>
        /// <param name="beginDay">beginning date's day</param>
        /// <param name="beginYear">beginning date's year</param>
        /// <param name="toMonth">target date's month</param>
        /// <param name="toDay">target date's day</param>
        /// <param name="toYear">target date's year</param>
        /// <returns>days between the input date</returns>
        public static int daysBetween(int beginMonth, int beginDay, int beginYear, int toMonth, int toDay, int toYear)
        {
            // This is pretty gross and i don't like to think about it, neither should you

            int numDays = 0; // Counter that's returned
            if (isMonthWithHoliday(beginMonth))
                numDays--;

            // If dates have the same year but not same month
            if (beginMonth != toMonth && toYear == beginYear)
            {
                while (toMonth != beginMonth)
                {
                    numDays += numDaysInMonthIncludingHolidays[beginMonth] - beginDay;
                    beginDay = 0;
                    if (++beginMonth > numMonthsInYear)
                    {
                        beginMonth = 1;
                        beginYear++;
                    }
                }
                numDays += toDay;
                if (isMonthWithHoliday(toMonth))
                    numDays++;
            }
            else if (beginMonth == toMonth && toYear == beginYear && toDay > beginDay)
            {
                numDays = toDay - beginDay;
            }
            else if (toYear != beginYear)
            {
                while (toYear - beginYear > 2)
                {
                    numDays += numDaysInYear;
                    beginYear++;
                }
                while (toMonth != beginMonth || toYear != beginYear)
                {
                    numDays += numDaysInMonthIncludingHolidays[beginMonth] - beginDay;
                    beginDay = 0;
                    if (++beginMonth > numMonthsInYear)
                    {
                        beginMonth = 1;
                        beginYear++;
                    }
                }
                numDays += toDay;
                if (isMonthWithHoliday(toMonth))
                    numDays++;

            }
            return numDays;
        }

        /// <summary>
        /// Calculates how many days between the 'begin' date and 'to' date
        /// </summary>
        /// <param name="beginDate">starting date formatted as MMDDYYYY</param>
        /// <param name="toDate">target date formatted as MMDDYYYY</param>
        /// <returns>days between the input date</returns>
        public static int daysBetween(string beginDate, string toDate)
        {
            return daysBetween(Int32.Parse(beginDate.Substring(0, 2)), Int32.Parse(beginDate.Substring(2, 2)), Int32.Parse(beginDate.Substring(4, 4)),
                Int32.Parse(toDate.Substring(0, 2)), Int32.Parse(toDate.Substring(2, 2)), Int32.Parse(toDate.Substring(4, 4)));
        }
        #endregion
    }
}
