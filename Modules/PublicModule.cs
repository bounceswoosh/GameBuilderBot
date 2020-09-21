﻿using Discord.Commands;
using DiscordOregonTrail.Services;
using System;
using System.Threading.Tasks;

namespace DiscordOregonTrail.Modules
{

    // Modules must be public and inherit from an IModuleBase
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        ResponseService ResponseService { get; set; }

        public PublicModule(ResponseService responseService)
        {
            ResponseService = responseService;
        }

        [Command("ping")]
        public Task PingAsync()
            => ReplyAsync("pong!");

        [Command("help")]
        public Task HelpAsync()
            => ReplyAsync(ResponseService.Help());



        [Command("game")]
        public Task TrailAsync(params string[] objects)
        => ReplyAsync(ResponseService.RollEvents(objects));


        [Command("state")]
        public Task StateAsync() => ReplyAsync(ResponseService.Dump());

        [Command("list")]
        public Task ListAsync() => ReplyAsync(ResponseService.GetAllChoices());


    }



}
