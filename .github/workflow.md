when working a new project the loop should look like this:
humans asks for starting the project
fundation step:

ai must help for the fundacional documentation and the architectural documentation (what languajes, what principles other that desing patterns, solid and kiss; requirements for the project)
have a small interview to set the arquitecture decision records (adr) over the fundacional docs
using openspec, start writting the docs
desing step:
ai must start to plan with the shortest vertical feature or the most critical one (ask the human)
use planotator so the human can review the plan
if there is something that is not clear, ask always the human
then, review if the plan is okey with the architectural docs - if there is a gap between the decision made and the architecture, create an adr with the human - if the plan collides with the requirements, or they are not met, review from the adr to the requirements if its reasonable with what has been stated step by step from adr, to principles to requirements
now that we have a plan, an specialist of backend or frontend must take action and follow the plan
if this implementation specialist thinks the plan is not clear enough, we must go back to the start of desing step
if the implementation specialist thinks something CANT be done, like 'making the pigs fly with fly's wings', ask for a better requirement analisys
now start breaking the plan in specs using OpenSpec
implementation step:
the implementator must start with the most critical spec
start implementing the tests - follow a TDD like
start with functionality
after spec is reached, ask the architect to review the code (the architect must give an ok if the code compiles, works as intended and doesnt has badsmells -- if solid, kiss and desging principles are not followed, is NOT OK; but if is needed not to follow them caused to languaje, is ok but try to minimize it - user will get angy and we dont want the human to be angry)
if the review is negative, fix the code and ask again -- do this until architect gives an ok
then, update the docs of the feature if its critical (like security or very high demanding) or configuration related
durable project memories/rules must be written in `.github/memories.md` so the repository stays self-describing when possible
implementations should be idempotent whenever it is feasible and reasonable for the feature
