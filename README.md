# Chess Engine
## Contents
Working Chess Game made using the unity engine. The project is made as a framework for creating Chess Agents in C#. The code is modular and allows unity users to create a Chess Agent with the built in functions of the framework and then test in in multiple ways.
### Creating Agents
1) Create a new C# script in the folder Assets/Scripts/Agents
2) Create the following methods:
* `public override void StartAgent(bool white)`
* `public override Move GetMove(Board board)`
* `public override float? GetEval(Board board)`
3) Add above the class `[CreateAssetMenu(fileName = "Name", menuName = "Agents/Name")]`
4) Create the asset (Create>Agents>Name) in the Assets/Agents folder.
### Testing frameworks and game
Different frameworks are available through different scens in Assets/Scenes.
Changing agents is done through the inspector in unity by adding the correct asset to the corresponding player field. 
#### Agent Comparer  
Runs a set amount of games between two different (or identical) agents. 
#### Game  
Runs a single game between two different (or identical) agents. During startup the user may pick which agent should be which colour, what timerules the game should be using and if an agents should instead be disable and replaced by human input.
#### Perft Test  
Tests the current move generators moves.
#### Magic  
Produces Magic bitboard numbers for chess programming optimisation. Press space to export current calculated magic bitboard number.
## Plans
### Agents
* V5 - Move Ordering
* V6 - Transposition Table
* V7 - Quiesence search
### Chess Programming
* Vector based move generation -> Bitboard pseudolegal solver.
* Board representation optimisation
* Rework player inputs.
* Resigning
* Draw votes
* Working UI buttons
* Pre Moves
* Right click to highlight squares or draw arrows for moves.