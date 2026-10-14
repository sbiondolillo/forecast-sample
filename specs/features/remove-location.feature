Feature: Removing a tracked location

  A person removes a tracked location by its id. The location leaves the
  database file, which the option --database names, and the other tracked
  locations keep their ids.

  Scenario: A tracked location is removed
    Given the database file holds the tracked locations
      | id | name      | latitude | longitude |
      | 1  | Kathmandu | 27.70169 | 85.3206   |
    When a person runs the program with "remove 1" on the database file
    Then standard output is "Removed Kathmandu (27.70169, 85.3206)"
    And the exit code is 0

  Scenario: A removed location is gone from the list
    Given the database file holds the tracked locations
      | id | name      | latitude | longitude |
      | 1  | Kathmandu | 27.70169 | 85.3206   |
    And a person ran the program with "remove 1" on the database file
    When a person runs the program with "list" on the database file
    Then standard output is "No tracked locations."
    And the exit code is 0

  Scenario: Removing one location leaves the others tracked
    Given the database file holds the tracked locations
      | id | name      | latitude | longitude |
      | 1  | Kathmandu | 27.70169 | 85.3206   |
      | 2  | Vaduz     | 47.14151 | 9.52154   |
    And a person ran the program with "remove 1" on the database file
    When a person runs the program with "list" on the database file
    Then standard output is "2 Vaduz (47.14151, 9.52154)"
    And the exit code is 0

  Scenario: An id that no tracked location has is refused
    Given the database file holds the tracked locations
      | id | name      | latitude | longitude |
      | 1  | Kathmandu | 27.70169 | 85.3206   |
    When a person runs the program with "remove 2" on the database file
    Then standard error has a line that says no tracked location has the id and holds "2"
    And standard output is empty
    And the exit code is 1

  Scenario: No database file means no tracked location to remove
    Given a database file that does not exist yet
    When a person runs the program with "remove 1" on the database file
    Then standard error has a line that says no tracked location has the id and holds "1"
    And standard output is empty
    And the exit code is 1
    And the database file still does not exist

  Scenario: The remove command lists the database option and its default
    When a person runs the program with "remove --help"
    Then standard output holds the option "--database" and its default value "forecast.db"

  Scenario: The usage in the README shows the remove command
    Then the section "Usage" of "README.md" holds the line "dotnet run --project src/App -- remove <id>"
