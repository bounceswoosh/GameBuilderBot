﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace GameBuilderBot.Models
{
    /// <summary>
    /// The Outcome object represents one of the possibilities available for a given <seealso cref="GameEvent"/>.
    /// For example, the GameEvent of Colors could contain Blue, Green, and Red Outcomes.
    /// Each of these could be given a weight - Maybe Blue is most likely (weight = 50),
    /// Green is next (weight = 40), and Red is least likely (10).
    /// 
    /// If Rolls is populated, its members are used to populate the Text when this Outcome
    /// occurs. For example, Blue's Rolls could be {"1d4", "1d8'} and Text could be
    /// "There are {0} blue ribbons and {1} blue buttons in the drawer."
    /// 
    /// If GameEvent is populated, this Outcome will result in another GameEvent being rolled, 
    /// which will result in a further Outcome, which might result in a further GameEvent ...
    /// </summary>
    public class Outcome
    {
        /// <summary>
        /// Short name (TODO could Text and Name be merged, or are there reasons to have a short name?
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Summed with the other Outcome members of a <seealso cref="GameEvent"/> to determine the
        /// likelihood of this outcome
        /// </summary>
        public readonly int Weight;

        /// <summary>
        /// Text to display when this outcome is rolled. <seealso cref="Rolls"/> may be used to 
        /// insert randomized values into Text. For example, Text might be set to
        /// "You see {0} emus and {1} llamas."
        /// </summary>
        public string Text;

        // TODO make this protected after moving rolls logic into here
        /// <summary>
        /// Rolls represent expressions or values to be inserted into Text when this outcome is rolled.
        /// Example values:
        /// "Sunrise" // Insert the current value of sunrise, or roll the value if it's not been set yet
        /// "!Sunrise" // Roll a new value for sunrise (and save it to the <seealso cref="GameState"/>
        /// "1d4" // Roll a four sided die for the value. Do not save it anywhere.
        /// </summary>
        public readonly string[] Rolls;

        // TODO Do I have to store the string as part of the Outcome object? Would rather not
        /// <summary>
        /// In order to avoid a proliferation of <seealso cref="GameEvent"/> objects, they are initially ingested
        /// as string members and then populated in a second pass. These should not be referenced
        /// after file load
        /// </summary>
        public readonly string GameEventAsString;

        /// <summary>
        /// Subsequent "roll" if the current Outcome is rolled. For example, if the Outcome is "Tree", this
        /// might be set to a <seealso cref="GameEvent"/> with Outcomes like "Oak", "Redwood", and "Pine"
        /// </summary>
        public GameEvent ChildGameEvent;

        /// <summary>
        /// Used to populate an Outcome using a <seealso cref="GameFile"/> as part of ingestion
        /// </summary>
        /// <param name="o"></param>
        public Outcome(OutcomeIngest o)
        {
            Name = o.Name;
            Weight = o.Weight;
            Text = o.Text;
            Rolls = o.Rolls;
            GameEventAsString = o.GameEventAsString;
        }

        /// <summary>
        /// Complete() should be called only after all GameEvents defined in the GameFile have been loaded.
        /// It will attempt to populate this Outcome's ChildGameEvent based on the value of the GameEvent string.
        /// This allows nested game responses without having to manually roll each related event.
        /// </summary>
        /// <param name="GameEventMap">A complete list of all GameEvents defined for the game.</param>
        public void Complete(Dictionary<string, GameEvent> GameEventMap)
        {
            if (GameEventAsString != null)
            {
                string key = GameEventAsString.ToLower();
                if (GameEventMap.ContainsKey(key))
                {
                    ChildGameEvent = GameEventMap[key];
                }
                else
                {
                    Console.WriteLine(
                        string.Format("**** WARNING: Outcome \"{0}\" of GameEvent \"{1}\" specifies child GameEvent \"{2}\"," +
                        " but \"{2}\" is not defined ****", Name, key, GameEventAsString));
                }
            }
        }

        public string GetResponse(GameState state)
        {
            string response;

            if (Rolls != null && Rolls.Length > 0)
            {
                var rolls = new List<int>();
                foreach (string expression in Rolls)
                {
                    rolls.Add(state.SetFieldValueByExpression(expression));
                }

                try
                {
                    response = string.Format(Text, rolls.Select(x => x.ToString()).ToArray());
                    response = string.Format("{0}", response);
                }
                catch (FormatException)
                {
                    response = "**!! Number of rolls specified does not match string format. Check your config file.!!**";
                }
            }
            else
            {
                response = Text;
            }

            return response;
        }
    }
}
