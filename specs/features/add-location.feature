Feature: Adding a tracked location

  A person adds a place by its name. The program asks the geocoding service
  of Open-Meteo for the places of that name and stores the one match.

  Background:
    Given a database file that does not exist yet

  Scenario: The program asks the geocoding service for the name
    Given the geocoding service answers with one result named "Kathmandu" at latitude 27.70169 and longitude 85.3206
    When a person runs the program with "add Kathmandu" on the database file
    Then the program sent the request "GET https://geocoding-api.open-meteo.com/v1/search?name=Kathmandu"

  Scenario: A name with one match is added
    Given the geocoding service answers with one result named "Kathmandu" at latitude 27.70169 and longitude 85.3206
    When a person runs the program with "add Kathmandu" on the database file
    Then standard output is "Added Kathmandu (27.70169, 85.3206)"
    And the exit code is 0

  Scenario: A name that no place has is refused
    Given the geocoding service answers with no result
    When a person runs the program with "add Zzzzqqqxxx" on the database file
    Then standard error has a line that says no place has the name and holds "Zzzzqqqxxx"
    And standard output is empty
    And the exit code is 1

  Scenario: A name with several matches is refused
    Given the geocoding service answers with several results
    When a person runs the program with "add Boston" on the database file
    Then standard error has a line that says the name matches several places and holds "Boston"
    And standard output is empty
    And the exit code is 1

  Scenario: A request that gets no answer is refused
    Given the geocoding service gives no answer
    When a person runs the program with "add Kathmandu" on the database file
    Then standard error has a line that says the service gave no answer
    And standard output is empty
    And the exit code is 1

  Scenario Outline: A failed add leaves the tracked locations as they were
    Given the geocoding service answers with one result named "Kathmandu" at latitude 27.70169 and longitude 85.3206
    And a person ran the program with "add Kathmandu" on the database file
    And the geocoding service <answer>
    When a person runs the program with "add <name>" on the database file
    Then standard output is empty
    And the exit code is 1
    When a person runs the program with "list" on the database file
    Then standard output is "1 Kathmandu (27.70169, 85.3206)"

    Examples:
      | answer                          | name       |
      | answers with no result          | Zzzzqqqxxx |
      | answers with several results    | Boston     |
      | gives no answer                 | Pokhara    |

  Scenario: A missing name sends no request
    Given the geocoding service answers with one result named "Kathmandu" at latitude 27.70169 and longitude 85.3206
    When a person runs the program with "add" on the database file
    Then the program sent no request
    And standard error is not empty
    And the exit code is not 0

  Scenario: The add command lists the database option and its default
    When a person runs the program with "add --help"
    Then standard output holds the option "--database" and its default value "forecast.db"
