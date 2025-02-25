# TO-DO

- [X] refine report that was initially submitted
- [ ] the ai will be used for the decision making of the AI

### Morgan

- [ ] Wait for me i.e. Morgan to get the NPCs working
- [ ] The AI will control the decision making for the NPC characters that play the games. What games they decide to play based on their current money and what is available to them. All decisions will be smart decisions done by the AI using the API instead of random ones based on RNG.
- [ ] Locally is fine for now
- [ ] No player login
- [ ] Yes it puts it back to where the user left off

# API To-Do List

- [X] Initialise Django project
- [X] Create response/ Django app
- [X] Create OpenAI API response scripts
- [X] Deploy API (connect to Unity game)
- [ ] Create API endpoints
  - [ ] Define endpoints for key actions (e.g., fetching game state, updating game state)
  - [ ] Ensure endpoints are accessible with correct parameters
- [ ] Create API documentation
  - [ ] Document all endpoints and their request/response formats
  - [ ] Provide examples for common API requests
- [ ] Create API tests
  - [ ] Write tests for endpoint responses
  - [ ] Simulate OpenAI API failures and ensure graceful handling
- [ ] Ensure the API is connected by running end-to-end tests
  - [ ] Verify Unity successfully sends requests to the Django API
  - [ ] Confirm Django API retrieves valid responses from OpenAI
  - [ ] Test if the final responses reach the Unity game as expected
