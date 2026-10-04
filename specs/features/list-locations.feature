Feature: Listing the tracked locations

  The tracked locations are in a SQLite database file, which the option
  --database names. They stay from one run of the program to the next.

  Background:
    Given a database file that does not exist yet

  Scenario: No file means no tracked locations
    When a person runs the program with "list" on the database file
    Then standard output is "No tracked locations."
    And the exit code is 0

  Scenario: A stored location is listed in a later run
    Given the geocoding service answers with one result named "Kathmandu" at latitude 27.70169 and longitude 85.3206
    And a person ran the program with "add Kathmandu" on the database file
    When a person runs the program with "list" on the database file
    Then standard output is "1 Kathmandu (27.70169, 85.3206)"
    And the exit code is 0

  Scenario: Locations are listed in the order they were added
    Given the geocoding service answers with one result named "Kathmandu" at latitude 27.70169 and longitude 85.3206
    And a person ran the program with "add Kathmandu" on the database file
    And the geocoding service answers with one result named "Vaduz" at latitude 47.14151 and longitude 9.52154
    And a person ran the program with "add Vaduz" on the database file
    When a person runs the program with "list" on the database file
    Then standard output has the lines
      """
      1 Kathmandu (27.70169, 85.3206)
      2 Vaduz (47.14151, 9.52154)
      """
    And the exit code is 0

  Scenario: The list command lists the database option and its default
    When a person runs the program with "list --help"
    Then standard output holds the option "--database" and its default value "forecast.db"
