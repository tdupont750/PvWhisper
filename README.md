# PvWhisper

A cross-platform Speech-to-Text console application.

Built using:
- [dotnet](https://dotnet.microsoft.com/en-us/download)
- [PvRecorder](https://github.com/Picovoice/pvrecorder)
- [Whisper.net](https://github.com/sandrohanea/whisper.net)
- [ydotool](https://github.com/ReimuNotMoe/ydotool)

Authored using AI via [JetBrains Junie](https://www.jetbrains.com/junie/)

## Features

- **Write to Console**
  - Prints transcribed Speech-to-Text to the console  
  - Works on Linux, Mac, and Windows

- **Write to Clipboard**
  - Copies transcribed Speech-to-Text to the system clipboard  
  - Works on Linux, Mac, and Windows

- **Write to Virtual Keyboard (ydotool)**
  - Types transcribed Speech-to-Text into a virtual keyboard  
  - Only available on Linux with [ydotool](https://github.com/ReimuNotMoe/ydotool)

- **Use any Whisper Model**
  - Multiple [Whisper Model](https://whisper-api.com/blog/models/) sizes supported
  - Automatically downloads models from [Hugging Face](https://huggingface.co/openai/models)

- **Toggle speech capture via named pipe**
  - Create a global shortcut to toggle speech capture via shell script
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

Configure by updating the JSON values in the [AppConfig.json](https://github.com/tdupont750/PvWhisper/blob/main/AppConfig.json) file.

Supported Outputs:
- `Console`
- `Clipboard`
- `ydotool`

See source for [OutputTargets](https://github.com/tdupont750/PvWhisper/blob/main/src/PvWhisper/Config/OutputTarget.cs) and [ModelKinds](https://github.com/tdupont750/PvWhisper/blob/main/src/PvWhisper/Config/ModelKind.cs).

Text transformations run in the order provided.

## Commands

Console Commands:
- `v` = toggle capture (start/stop + transcribe)
- `c` = start capture
- `z` = stop capture and discard audio
- `x` = stop capture and transcribe
- `q` = quit

In another terminal, you can send commands:
- `echo -n 'v' > "$PIPE_PATH"`   # toggle capture
- `echo -n 'q' > "$PIPE_PATH"`   # quit

## License

Released under the GNU General Public License v3.0.

# Adventures in Vibe Coding

This section is the story of why I built this tool, how I built this tool, and the lessons I learned while building it.

(And yes, this was typed entirely using this Speech-to-Text program and then passed through ChatGPT for spelling and grammer fixes.)

## The morality elephant in the room

To get this out of the way up front: there are a lot of very valid concerns around AI, let alone the moral conundrums related to generative AI.

I am worried about these things. I am worried about the privacy issues of data scraping. I am worried about the violation of intellectual property. I am worried about the morality and misinformation of deepfakes. I am worried about the potential job losses and social impacts of this level of automation. I am worried about all of these things.

And yet we have to face the reality that these things are here; and, like them or not, they are here to stay.

As such, I encourage people to research these tools and learn how to use them. If we don't understand these tools or how to use them, then it is very likely they will be used against us; whether through the dissemination of misinformation or through the automation and replacement of individuals who refuse to learn the new tools of their vocation.

In closing, I respect any concerns you might have regarding AI, and I wanted to disclose not only that I use these tools but why I chose to do so.

## Backstory

*(Scroll below if you just want to read about the learnings from this adventure.)*

### My hand injury

I've been programming since seventh grade and programming professionally for the last 20 years. Unfortunately, three years ago, I popped a tendon in my thumb and, despite physical therapy and attempts to take care of it, the injury progressively worsened. This culminated in surgery to clean out and tighten the ball joint of my thumb.

My thumb works well again, but is very easy to irritate; especially during long, repetitive activities such as typing. This led me to explore many Speech-to-Text alternatives to drastically reduce my day-to-day typing.

### My first attempt at speech-to-text

I was amazed at how few directly integrated Speech-to-Text services there were in modern operating systems. Even more shocking was that the computer alternatives were absolutely terrible. By far the best Speech-to-Text I found was Google's Gboard voice input for Android. But that had no native PC support, and as a privacy advocate I wasn’t a big fan of being forced to use that for all of my day-to-day communication.

So, like any typical software engineer, I set out to build my own Speech-to-Text solution. (Because I’m sure that my wheel would be rounder.)

My first attempt, which I used for over a year, involved getting an old Android phone, installing the FUTO keyboard on it, and building a web page that would send the transcribed text to a web server running on my computer, where it would then be piped into the keyboard. That setup, paired with a small hardware macro keyboard, made me infinitely more productive, and I was quite pleased with the solution for a long time.

TL;DR: spare Android phone with an open-source keyboard → web server → clipboard → pasted with macro keyboard. (That's a lot of steps.)

### Starting to use AI coding agents

As I said above, I feel it's important to learn these tools and practice new skills.

I had been using ChatGPT in small ways to help with one-off functions and somewhat advanced bash scripts. AI is remarkably good at writing shell commands; until it starts hallucinating and making up command-line arguments.

My first significant foray into using AI tools for programming was learning Android development. Having ChatGPT answer questions, provide links to documentation, and offer analogies to languages I already knew was incredibly helpful. Some time later, I attended an AI workshop where a close friend gave me an overview of how he uses AI tools to help with his work; it was both impressive and enlightening.

This prompted me to go home and spend a weekend developing this project by attempting to use only AI. Spoilers: it was a resounding success.

## What I have learned about vibe coding

If you take away anything from this write-up, let it be this analogy:

> **Generative AI is not a 3D printer. It's a pottery wheel.**

You cannot simply feed an AI one well-crafted set of plans and expect to get a highly functional result out the other side. The output requires constant refinement. The user needs to understand the output and continuously refine the prompts to craft a meaningful outcome.

Sometimes the pottery wheel spins too slow or too fast, sometimes it collapses, and sometimes you simply have to delete the whole file (preferably via `git reset`) and start over; but that’s how you learn to grow your context.

### Understanding intent

The most important thing to remember is this: no AI will understand the intent behind what you are trying to accomplish.

AI can understand the immediate task. It can statistically extrapolate the next step based on what other people (in its training data) commonly do. But it will miscalculate. It will hallucinate. It will veer off course. The longer you wait to intervene, the worse the results get.

Because of this, AI is not good at thinking about the big picture or architecture. A large language model can only hold so much context in memory at once; this is something where the human brain still does have quite an advantage over our GPU counterparts! While AI is phenomenal at understanding one method or one class, it is not going to understand the whole project, let alone an entire solution.

### What AI coding agents are good at

It may sound like a short list, but it’s really not:

1. AI is genuinely phenomenal at reading documentation.  
2. AI is remarkably good at completing single tasks, crafting single classes, and authoring single functions.  
3. AI is usually quite good at small refactors.

Think about the years of education and experience required for a human to learn, letalone master these tasks. In theory, an effectively infinite army of AI agents is standing by, ready to do this work. In theory, they can onboard into your codebase in minute. Objectively, when compared to a human salary, cloud compute is cheap.

An AI coding agent is like an engineer with encyclopedic knowledge of the tech stack it’s working in, and after a few seconds of “thought,” it can type at world-record speed.

Okay, now for the downsides.

### What AI coding agents are bad at

Let’s get the hyperbolic sentence out of the way: AI agents are savant sociopaths who will lie to your face and even argue with you in order to try to make you happy.

From here, I’ll use concrete examples from this project to discuss the shortcomings of AI coding agents.

#### Occasionally it just lies (hallucinates)

At the start of the project, I asked ChatGPT to pick libraries. It wisely recommended Whisper.net and TextCopy (which I already planned to use). Originally, it recommended [NAudio](https://github.com/naudio/NAudio) for audio. While NAudio is cross-platform, it does **not** support microphone input on Linux, despite the explicit requirement for cross-platform audio input.

#### Everything is treated as a one-off script

At the start, I told ChatGPT I wanted to work on a new project, so it generated shell scripts that literally created a new .NET project and added dependencies. Nice for beginners, not useful for maintaining a real project.

When adding Whisper.net dependencies, it didn’t include `Whisper.net.Runtime`, so although the project compiled, it wouldn’t run.

#### Monolithic methods and classes

The AI’s only real objective is to complete the immediate task, so it treats everything like a script and produces monstrous methods.

`Program.Main` kept growing:
- Initialization  
- Console help  
- Input loops  
- Output writing  
- *Everything*

I had to repeatedly stop and explicitly instruct the AI to refactor and break functionality out into different methods and classes.

#### Statics everywhere

After initial refactors, everything was still static. I had to explicitly instruct it to remove statics, create interfaces, and use constructor injection.

#### Design patterns

AI almost never uses design patterns unless explicitly told to. Everything defaults to giant methods full of nested loops and conditionals.

The `CommandSources` and `OutputDispatchers` were originally in the same class. Even when asked to break them apart, it didn’t create a factory; it just instantiated everything inline.

#### Testability

AI is shockingly good at authoring unit tests with edge cases; but only when the code itself is testable.

Ironically, it’s terrible at writing testable code unless explicitly instructed to. For example, I had to tell it to move the RegEx replacement functionality into its own testable class.

#### Large refactors

It’s good at refactoring a single method or class, but struggles across classes. When dealing with lambdas and closures, it frequently adds unnecessary pass-throughs or wrappers.

It often won’t delete dead code unless explicitly told to. It also leaves behind old methods with comments about “backwards compatibility,” and doesn’t mark them obsolete. (This definitely implies that it's being trained on SDKs which intentionally leave legacy methods behind.) 

#### Does not intuit SDK edge cases

The .NET Console library is a notoriously weak part of the framework. Particularly problematic is that many read calls block even when marked async. The AI trusts the documentation too literally and doesn’t anticipate blocking.

This caused:

#### Only solving the first problem

Reading from a named pipe blocks until something arrives. If no input is ever received, then this will cause a deadlock when shutting down.

The AIs first solution: wrap in a timeout and skip disposing if it didn’t respond, which is just ignoring the root problem.  
The AIs second: open the pipe as read/write, which fixed the first block but then immediately caused another block on read.  
The AIs third: fall back to the timeout again.  
The AIs fourth: generate OS-specific native code via extern methods; surprisingly effective, but not desirable.

#### Repeats the same mistakes

I had a much simpler solution in mind: write something into the pipe at creation time so .NET has something to read.

When asked to update my shell script, it immediately wrote to the pipe, which then caused the exact same deadlock for the writer.

Only after instructing it to add a timeout to the shell scripts write did it finally work.

### Key takeaways

You may have noticed a recurring word:

> **Explicitly**

You must tell AI coding agents exactly what you want at a granular level.

- If you don’t know design patterns, it won’t use them.  
- If you can’t identify inefficiencies, it won’t fix them.  
- If you don’t know the edge cases, it won’t handle them.

You must *constantly* force refactoring:

- Break apart methods.  
- Break apart classes.  
- Write unit tests.

### Closing thoughts

**The Good:** AI coding agents can make programmers significantly faster.

**The Bad:** …but only if those programmers already understand best practices, architecture, and design patterns.

**The Ugly:** I am genuinely terrified about how many monolithic, unsustainable codebases are going to be filled with AI-generated slop.

But, like all things, practice will make perfect... so start practicing!
