# TO-DO

- [X] refine report that was initially submitted
- [ ] the ai will be used for the decision making of the AI

### Morgan

- [ ] Wait for Morgan to get the NPCs working
- [ ] The AI will control the decision making for the NPC characters that play the games. What games they decide to play based on their current money and what is available to them. All decisions will be smart decisions done by the AI using the API instead of random ones based on RNG.
- [ ] Locally is fine for now
- [ ] No player login
- [ ] Yes it puts it back to where the user left off

# API To-Do List

- [X] Initialise Django project
- [X] Create response/ Django app
- [X] Create OpenAI API response scripts
- [X] Deploy API (connect to Unity game)
- [X] Create API endpoints
- [X] Create API documentation
  - [X] Document all endpoints and their request/response formats
  - [X] Provide examples for common API requests
- [ ] Create API tests
  - [ ] Write tests for endpoint responses
  - [ ] Simulate OpenAI API failures and ensure graceful handling
- [X] Ensure the API is connected by running end-to-end tests
  - [X] Verify Unity successfully sends requests to the Django API
  - [X] Confirm Django API retrieves valid responses from OpenAI
  - [X] Test if the final responses reach the Unity game as expected
