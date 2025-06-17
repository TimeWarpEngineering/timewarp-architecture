# Defintion of Done

`*` indicates required.

## Api Endpoint

- [ ] Implementation
  - [ ] Server
    - [ ] *Endpoint
    - [ ] Server side only Validator
    - [ ] Mapper 
    - [ ] *Handler 
  - [ ] Api
    - [ ] *Request
    - [ ] *Response
    - [ ] *RequestValidator
- [ ] Integration Tests (Fixie)
  - [ ] *Handler Tests
    - [ ] *returns a valid Response given a valid Request via Handler
  - [ ] *Endpoint Tests
    - [ ] *returns valid http Response given valid http Request via Endpoint
    - [ ] *Should throw a validation error given invalid Request (only need to test one validation rule)
  - [ ] *RequestValidator Tests (Test all validation rules)

- [ ] Documentation
  - [ ] *Request class and properties
  - [ ] *Response class and properties

## Client Feature
- [ ] Implementation
  - [ ] *State
  - [ ] Actions
  - [ ] Pipeline
  - [ ] Notification
  - [ ] Components
  - [ ] Pages
- [ ] Integration Tests
  - [ ] State
    - [ ] ShouldClone
    - [ ] ShouldSerialize (To support Redux DevTools)
  - [ ] Every Action should have at least a positive test
- [ ] End-to-end Tests
  - [ ] Test each Page can at least render without error given valid states.
  - [ ] Test happy paths for each primary use case.

