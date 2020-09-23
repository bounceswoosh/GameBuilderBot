﻿using Discord.Commands;
using DiscordOregonTrail.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscordOregonTrail.Services
{
    public class ResponseService
    {
        private readonly Config _config;

        public ResponseService(Config config)
        {
            _config = config;
        }

        public string Help()
        {
            var sb = new StringBuilder()
                .AppendLine("> **Help:**");

            foreach (string k in _config.choiceMap.Keys)
            {
                Choice c = _config.choiceMap[k];
                if (c.IsPrimary)
                {
                    sb.AppendLine(String.Format("`!game {0}`: {1}", k, c.Description));
                }
            }

            return sb.ToString();
        }


        public Task Summarize (SocketCommandContext context)
        {
            StringBuilder sb = new StringBuilder();

            foreach (Choice c in _config.choiceMap.Values)
            {
                sb.AppendLine(c.GetSummary());
            }

            string response = sb.ToString();

            if (response.Length < 2000)
            {
                return context.Channel.SendMessageAsync(response);
            } else
            {
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                writer.Write(response);
                writer.Flush();
                stream.Position = 0;

                string fileName = string.Format("{0}_summary.txt", context.Channel.Name);
                return context.Channel.SendFileAsync(stream, fileName, "The summary is too long to send as a DM; " +
                    "sending it to you as a file instead.");

            }
        }

        public Task Export(SocketCommandContext context, string format)
        {
            string response = "";
            string error = "";

            format = format.ToLower();

            switch (format)
            {
                case ("yaml"):
                    response = GetYaml();
                    break;
                case ("json"):
                    response = GetJson();
                    break;
                default:
                    error = "Unrecognized file format request";
                    break;
            }

            if (error.Equals(""))
            {
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                writer.Write(response);
                writer.Flush();
                stream.Position = 0;

                string fileName = string.Format("game.{0}", format);
                return context.Channel.SendFileAsync(stream, fileName, "Here you go!");

            }
            else
            {
                return context.Channel.SendMessageAsync(error);
            }

        }

        protected string GetJson()
        {
            List<ChoiceExport> export = GetExport();

            var options = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                WriteIndented = true
            };
            return JsonSerializer.Serialize(export, options);


        }

        protected string GetYaml()
        {
            List<ChoiceExport> export = GetExport();
            return new YamlDotNet.Serialization.SerializerBuilder().Build().Serialize(export);
        }

        private List<ChoiceExport> GetExport()
        {
            var export = new List<ChoiceExport>();
            foreach (Choice c in _config.choiceMap.Values)
            {
                export.Add(new ChoiceExport(c));
            }

            return export;
        }

        public string RollEvents(params string[] objects)
        {
            string response = Help();

            if (objects.Length == 0 || objects[0].ToLower().Equals("help")) return response;

            string choice = objects[0];

            response = "> " + GetResponse(choice, 0);

            return response;
        }

        public string GetResponse(string choice, int depth)
        {
            depth++;

            if (depth > 20)
            {
                return "Too much nesting detected. Check your config file. Aborting!";
            }

            StringBuilder sb = new StringBuilder();

            if (_config.choiceMap.ContainsKey(choice.ToLower()))
            {
                sb.AppendLine((string.Format("**{0}**", choice)));
                Choice c = _config.choiceMap[choice.ToLower()];

                switch (c.Distribution)
                {
                    case "Weighted":
                        sb.Append(GetWeightedResponse(c, depth));
                        break;
                    case "All":
                        sb.Append(GetAllResponse(c, depth));
                        break;
                    default:
                        sb.AppendLine(String.Format("\"{0}\" has an invalid Distribution", choice));
                        break;
                }

            }
            else
            {
                sb.AppendLine(String.Format("unrecognized parameter **`{0}`**", choice));
            }

            if (sb.Length == 0)
            {
                sb.AppendLine("Something went wrong");
            }
            return sb.ToString();


        }

        private StringBuilder GetAllResponse(Choice c, int depth)
        {
            StringBuilder sb = new StringBuilder();

            foreach (Outcome o in c.Outcomes)
            {

                if (o.ChildChoice == null)
                {
                    sb.AppendLine("\t" + GetOutcomeResponse(o));
                }
                else
                {
                    sb.Append(GetResponse(o.ChildChoice.Name, depth));
                }
            }

            return sb;
        }

        private string GetOutcomeResponse(Outcome o)
        {

            string response;

            if (o.Rolls != null && o.Rolls.Length > 0)
            {
                var rolls = new List<int>();
                foreach (string expression in o.Rolls)
                {
                    rolls.Add(DiceRollService.Roll(expression));
                }

                try
                {
                    response = String.Format(o.Text, rolls.Select(x => x.ToString()).ToArray());
                    response = String.Format("**{0}**", response);
                }
                catch (FormatException)
                {
                    response = "**!! Number of rolls specified does not match string format. Check your config file.!!**";
                }
            }
            else
            {
                response = o.Text;
            }

            return response;

        }

        private StringBuilder GetWeightedResponse(Choice c, int depth)
        {
            StringBuilder sb = new StringBuilder();

            int max = c.PossibleOutcomes.Length;
            int roll = DiceRollService.Roll(max) - 1;

            Outcome o = c.GetOutcome(c.PossibleOutcomes[roll]);

            string outcome = GetOutcomeResponse(o);

            if (max <= 1)
            {
                sb.AppendLine(outcome);
            }
            else if (max == 2)
            {
                sb.AppendLine(string.Format("Flipped a coin and got: **{0}**", outcome));
            }
            else
            {
                sb.AppendLine(string.Format("[1d{0}: {1}] **{2}**", max, roll + 1, outcome));
            }

            if (o.ChildChoice != null)
            {
                sb.Append(GetResponse(o.ChildChoice.Name, depth));
            }

            return sb;
        }

        public string GetAllChoices()
        {
            StringBuilder sb = new StringBuilder()
            .AppendLine("> List all possible `!trail` arguments");

            var list = _config.choiceMap.Keys.ToList();
            list.Sort();

            foreach (string choice in list)
            {
                sb.AppendLine(String.Format("`{0}`", choice));
            }

            return sb.ToString();
        }

    }
}
