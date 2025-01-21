# TO-DO

- [X] refine report that was initially submitted

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