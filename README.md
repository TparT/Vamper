# Vamper

![VamperLogo](https://github.com/TparT/Vamper/assets/66745515/e798687f-c67c-4dd5-9410-ecff203374c6)


## The problem

The challange was brought to me by a friend who manages a YouTube VODs channel for Philza (Minecraft Twitch Streamer). They asked me if its possible to detect when Philza changes the stream title during stream, like when he switches to another gamemode in the same game, so it will be easier to find when he switched his gamemode later in the VOD.

The current way of how Twitch timestamps work is that they are only created when the streamer changes their stream catagory. Or when the streamer manually creates a marker with a description.

Now, since Philza stays on the same catagory and never creates markers.. no timestamps are made! And my friend had to manually look for when Philza switched to another gamemode, which often takes a very long time to find the exact timestamp.

So, my friend asked me for help by asking for a tool that will detect when Philza changes his stream title when he wants to show that he is doing something else on the same game than what the previous title stated.

## The solution

Vamper to the rescue!

The way Vamper works is by listening to Twitch's websocket events API, and it looks for title changes and reports them to the user by showing the previous title, and the new title,
and most importantly, it calculates the timestamp of when the title change happened.

For example, when Philza streams, he usually plays only Minecraft through the whole stream,
(Which means his catagory won't change) And he would start the stream with his Minecraft Hardcore survival world, and after a while he would join a multiplayer server with his friends (Currently he joins the QSMP server).

So, when he switches from Minecraft Hardcore to a Multiplayer server like the QSMP,
the title of the stream would be:
"Hardcore boi go brrrrrrrrrrrrrrrr 1.19.3 - qsmp later yesyes - !update !s4 !project !gg"

And then later, when he switches to QSMP, he will change the title to something like:
"QSMP - Tallulah's lil bday :)"

Vamper will then detect that an update was made and see if the old title matches the new title.
If they don't match, it means that a title update was made and it would then calculate and provide the timestamp of when he changed his title.

In addition, all the updates that are made to the stream are logged into a CSV file, that can be opened using Excel, to sort and organize all the obtained data such as:

Time (UTC), Streamer Name, Event Type, Stream Catagory Name, Stream Catagory Id, Stream Title, Stream TimeStamp, VOD Link, Stream Id.

### **_IN ORDER FOR VAMPER TO WORK MAKE SURE YOU:_**

- Make sure to add client id and access token in the config.json file otherwise it wont work!

- You can get the client id and access token from this website: https://twitchtokengenerator.com/
   basically select 'bot chat token' although Vamper is not a bot but whatever Okayge,
   and log in with a Twitch account, doesn't have to be your main account, any account will work just fine.
   and then scroll down and click the green "Generate Token!" button.
   Then authorize, and your client id and access token should be infront of you :D

- Put the correct channel id in the config.json file of the channel you want to monitor!

- Enjoy Vamper the timestamper! catKISS

### Special thanks to:
The [@TwitchLib](https://github.com/TwitchLib) C# Library Devs for developing [TwitchLib](https://github.com/TwitchLib/TwitchLib) to handle with the Twitch API.

[@JoshClose](https://github.com/JoshClose) for developing [CsvHelper](https://github.com/JoshClose/CsvHelper) to handle with CSV files.

#### Signed
~ [@T-par-T](https://github.com/TparT), Creator of [Vamper](https://github.com/TparT/Vamper).
