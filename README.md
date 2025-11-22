# PvWhisper

A cross platform Speech to Text console application.

Build using:
- [dotnet](https://dotnet.microsoft.com/en-us/download)
- [PvRecorder](https://github.com/Picovoice/pvrecorder)
- [Whisper.net](https://github.com/sandrohanea/whisper.net)
- [ydotool](https://github.com/ReimuNotMoe/ydotool)

Authored using AI via [JetBrains Junie](https://www.jetbrains.com/junie/)

## Features

- **Write to Console**
	- Prints transcribed Speech to Text out to the console
	- Works on Linux, Mac, and Windows

- **Write to Clipboard**
	- Copy transcribed Speech to Text to the system clipboard
	- Works on Linux, Mac, and Windows

- **Write to Virtual Keyboard (ydotool)**
	- Type transcribed Speech to Text into a virtual keyboard
	- Only available on Linux with [ydotool](https://github.com/ReimuNotMoe/ydotool)  

- **Use any Whisper Model**
	- Multiple [Whisper Model](https://whisper-api.com/blog/models/) sizes supported
	- Automatically downloads models from [Hugging Face](https://huggingface.co/openai/models)

- **Toggle speech capture via named pipe**
	- Create global shortcut to toggle speech capture via shell script
	- Works on Linux, Mac, and Windows with [WSL](https://learn.microsoft.com/en-us/windows/wsl/install)

- **Text transformations with Regular Expressions**
	- Create any number of transform rules
	- Use plain text or regular expressions

## Getting Started

1. Install the [dotnet SDK](https://dotnet.microsoft.com/en-us/download)
2. Clone this repository
3. Configure via `AppConfig.json` 
4. Execute `run_pvw.sh` 
5. Toggle capture by executing `toggle_pvw.sh`

## Configuration

Configure by updating the JSON values found in the [AppConfig.json](https://github.com/tdupont750/PvWhisper/blob/main/AppConfig.json) file

Supported Outputs:
- `Console`
- `Clipboard`
- `ydotool`

See source for [OutputTargets](https://github.com/tdupont750/PvWhisper/blob/main/src/PvWhisper/Config/OutputTarget.cs) and [ModelKinds](https://github.com/tdupont750/PvWhisper/blob/main/src/PvWhisper/Config/ModelKind.cs)

Text transformations will run in the order provided

## Commands

Console Commands:
- `v` = toggle capture (start / stop + transcribe)
- `c` = start capture
- `z` = stop capture and discard audio
- `x` = stop capture and transcribe
- `q` = quit

In another terminal, you can send commands:
- `echo -n 'v' > '$PIPE_PATH'   # toggle capture`
- `echo -n 'q' > '$PIPE_PATH'   # quit`

## License

Release under the GNU General Public License v3.0

# Adventures in Vibe Coding

This section is the story of why I built this tool, how I built this tool, and the lessons I learned while building this tool.

(And yes, it was typed entirely using this speech to text program!)

## The morality elephant in the room

To get this out of the way up front: there are a lot of very valid concerns AI, let alone the moral conundrums around generative AI. 

I am worried about these things. I am worried about the privacy of data scraping. I am worried about the violation of intellectual property. I am worried about the morality and misinformation of deepfakes. I am worried about the potential job losses and social impacts of this level of automation. I am worried about all of these things. 

And yet we have to face the reality that these things are here; and like them or not, they are here to stay.

As such, I encourage people to research these tools and learn how to use them. If we don't understand these tools or how to use them, then it is very likely they will be used against us; be that by the dissemination of misinformation, or automation and replacement of individuals who refuses to learn the new tools of their vocation.

In closing, I respect any concerns you might have regarding AI, and thus I wanted to disclose not only that I use those tools but why I chose to do so.

## Backstory

(Scrol below if you just want to read about the learnings from this adventure.)

### My hand injury

I've been programming software since seventh grade and programming professionally for the last 20 years. Unfortunately, three years ago, I popped a tendon in my thumb and despite physical therapy and trying to take care of it that turned into a progressively worse injury. This culminated in a surgery to clean out and tighten up the ball joint of my thumb.

My thumb works well again, but is very easy to irritate; especially during long repetitive activities, such as typing. This facilitated me to look into a lot of speech to text alternatives to help drastically reduce my day to day typing. 

### My first attempt at speech to text

I was amazed how few directly integrated speech to text services there were in our modern operating systems. Even more shocking was that the computer alternatives were absolutely terrible. By far the best speech to text was Google's G-Talk keyboard for Android. But that had no native support for PC and as a privacy advocate I wasn't a big fan of being forced to use that for all of my day to day communication.

So like any typical software engineer, I set out to build my own speech-to-text solution. (Because I'm sure that my wheel would be rounder.)

My first attempt that I used for a over a year was actually to get an old Android phone, install the FUTO keyboard on it, and then make a web page that would send the transcribed text to a web server running on my computer, where it would then be piped that into the keyboard. That software paired with a little hardware macro keyboard made me infinitely more productive, and I was quite pleased with the solution for a long time.

TLDR: from spare Android phone with an open source keyboard, to a web server, to the clipboard, and finally pasted with macro keyboard. (That's lot of steps.)

### Starting to use AI coding agents

As I said above, I feel it's important to learn these tools and practice new skills.

I had been using ChatGPT little bits here and there to help with writing one-off functions and somewhat advanced bash scripts. AI is actually remarkably good at writing shell commands until it starts hallucinating and making up command line arguments.

My first significant foray into using AI tools for programming was learning how to do Android development. Having ChatGPT answered questions for me, provide links to documentation, and offer analogies to other languages that I was already familiar with, was incredibly helpful. Some time later I attended an AI workshop where one of my close friends gave me an overview of how he is using Cloud Code to help with his work; it was both remarkably impressive and enlightening.

This prompted me to go home and spend the weekend developing this project by attempting to use only AI. Spoilers: it was a resounding success.

## What I have learned about vibe coding

If you take away anything from reading this write up, let it be this analogy: 

> **Generative AI is not a 3D printer. It's a pottery wheel.**

You cannot simply feed an AI one set of plans (no matter how well crafted) and expect to get out a highly functional result out the other side. The output requires constant refinement. The user needs to understand the output and continuously refine the prompts to craft a meaningful outcome.

Sometimes the pottery wheel is going to spin too slow or too fast, sometimes it's going to explode, and sometimes you're going to have to delete the whole file (preferably git reset) and start over; but that's just how you learn to grow your context.

### Understanding intent

I think the most important thing to keep in mind is this: no AI will understand the intent of what you are trying to accomplish. 

AI can understand the immediate task that you are working on. It can then statistically extrapolate the next step that other people (it's training data) commonly take, and so on and so forth. But ultimately it is going to miscalculate, it is going to hallucinate, it will veer off course; the longer you wait to intervene, the worse the results are going to get.

Because of this, AI is not good at thinking about the big picture or architecture. A large language model can only hold so much context in memory at once; this is something where the human brain still does have quite an advantage over our GPU counterparts. While the AI is phenomenal at understanding one method or one class, it is not going to understand the whole project let, let alone an entire solution.

### What AI coding agents are good at

This may sound like a short list, but it's really not.

1. AI is genuinely phenomenal at reading documentation. 
2. AI is remarkably good at completing single tasks, crafting single classes, and authoring single functions. 
3. AI is usually quite good at handling small refactors.

Think about the hours of education and subsequent number of years someone has to spend programming in order to understand and become efficent at completing these types of tasks. And effectively infinite army of AI agents are standing by and ready to do this work for you and your company; they're available to start work right now, they can onboard into your codebase in minutes, and compare to a human salary their cloud compute is cheap.

So we need to acknowledge that the value here is truly quite high. An AI coding agent is like engineer who has an encyclopedic knowledge of the technology stack it'sworking in, and after a few seconds of "thought" it can then type at a world record level of key strokes per minute.

Okay, let's get to the down sides.

### What AI coding agents are bad at

Let's get the hyperbolic fun sentence out of the way so that we can talk about more substantive topics: all AI agents are savant sociopaths who will light your face and even argue with you in order to try to make you happy.

**From here I will use discrete examples from developing this project in order to discuss the shortcomings of AI coding agents.**

#### Occasionally it just lies (hallucinates)

At the start of the project, I asked ChatGPT to pick out different libraries for the project. It wisely recommended Whisper.net and TextCopy (both of which I already planned on using), originally the AI recommended that I use [NAudio](https://github.com/naudio/NAudio) for audio. While NAudio is cross-platform, it does not support microphone input on Linux; and this was recommended despite being asked explicitly to find a cross-platform audio input solution.

#### Everything is treated as a one off script

At the start of the project I told ChatGPT that I wanted to work on a new project, and so it created shell scripts to literally create a new .NET project and add new dependencies to it. That is really nice for a beginner, but not particularly useful to maintaining a project.

When it added dependencies for Whisper.net, it did not know to include the Whisper.net.Runtime package. So while I was able to code and compile, the project would obviously not execute at runtime without the runtime package.

#### Monolithic methods and classes

The AI's only real objective is to complete its immediate task, and thus it will treat everything like a script instead of a program, authoring absolutely monstrous methods and classes. 

Program.Main just continued to grow larger and larger throughout the development of the project:
- Main initialized everything.
- Main included all the console help documentation.
- Main created the loops for reading input.
- Main handled writing all of the outputs.
- Main was absolutely massive. 

I had to continuously stop and explicitly instruct the AI to refactor by breaking out different types of functionality into different methods and classes.

#### Statics everywhere

After refactoring and moving pieces out of Main, literally all classes were still statically linked to each other. I had to explicitly tell the AI to create interfaces for the different types, remove the static methods, and inject the interfaces via constructor injection.

#### Design patterns

AI prompt tools almost never default to trying to use a design pattern unless explicitly told to do so. This results in almost every single first pass generating gigantic methods full of nested loops and if statements.

The CommandSources and OutputDispatchers were all originally in the same class. Even when explicitly instructed to break out the implementations, it still did not create a factory; instead it instantiated new instances of each class inline every single time they were invoked.

#### Testability

AI is shockingly good at creating unit tests that include edge and boundary cases in the test plan. It is genuinely awesome when the AI reruns the tests, sees it something broke, and then refactors to fix the failures automatically.

Ironically, the AI is atrocious at writing testable code on it's own. I had to explicitly instruct the agent to move the RegEx replacement into its own class in order to be tested.

#### Large refactors

The AI was very effective at refactoring single methods or classes. It can add and remove properties and automatically bind them to the configuration with ease.

When refactoring across classes, it really starts to struggle. If there is any context that it needs, such as understanding when a closure is able to wrap a variable, then it almost always generatedin inefficient pass-throughs and wrappers that were completely unnecessary.

Additionally, it would often leave behind old methods or code, even commenting them saying that it was leaving it for backwards compatibility purposes. This definitely implies that it's being trained on SDKs that intentionally leave legacy methods behind; and yet it did not mark them with obsolete attributes. It would not delete this dead code unless explicitly told to do so.

#### Does not intuit SDK edge cases

The .NET console library, despite being at the core of .NET itself since version 1.0, is a notoriously weak SDK. For example: many read calls often block, even when marked as async. The AI obviously trusts the documentation which claims that these calls are asynchronous, and thus does not account for when they may block.

It's actually a result of a fascinating deadlock problem...

#### Only solves the first problem

When reading from a named pipe, .NET's read operation will block until there is something in the pipe to read. This means that if no data is fed to the pipe and before the app is asked to shut down, then it will deadlock waiting for the read operation to complete.

When told about the deadlock, its first attempt to solve the problem was merely to wrap the whole thing in a timeout and just not bother disposing of the file reader if it didn't respond in five seconds. When told explicitly that it was reading from a named pipe, it then intuited that the file reader needed to be made read right in order to not block on open; however, this immediately led to a blocking call on the first read operation. When I told him about that, it merely fell back to its original solution and restored the timeout code.

It's final attempt to solve the problem was quite a surprise, and actually worked remarkably well: it generated extern methods to invoke OS specific native libraries. As cool as that was, I didn't want to have references to external dependencies that offered bespoke solutions for different operating systems...

#### Repeats the same mistakes

I had a much simpler solution to the pipe problem: just write something to the pipe when it's created, and then dotnet will have something to read when it initializes. When explicitly instructed to update my shell script, the AI wrote out to the pipe only to experience the exact same issue of blocking until something was ready to read.

When instructed to explicitly add a timeout to that right, everything worked as it should.

### Key take aways

You may have noticed the repeated use of a specific word throughout this document:

> Explicitly

You need to be equipped to tell AI coding agents exactly what you want them to do on a very granular level.

- If you don't know design patterns, it won't use them.
- If you can't identify memory inefficiencies, it won't address them.
- If you are unaware of edge cases, it won't solve them.

Beyond that, you need to constantly instruct the AI agents to refactor the code.

- Instruct it to break apart methods.
- Instruct it to break apart classes.
- Instruct it to write unit tests.

### Closing thoughts

**The Good:** AI coding agents will be able to make programmers faster...

**The Bad:** ...but only if they understand best patterns and practices, general architecture and design patterns, and the tools that they are using.

**The Ugly:** I am genuinely terrified about How many monolithic and unsustainable code bases are going to be filled with with AI generated slop.

But, like all things, practice is going to make perfect; so start practicing!
