Feature: Launching the program

  A person launches the built console program as a process. The program
  starts and runs the command that its arguments name.

  Background:
    Given a database file that does not exist yet

  Scenario: The launched program shows its help
    When a person launches the program with "--help"
    Then the exit code is 0

  Scenario: The launched program lists the tracked locations
    When a person launches the program with "list" on the database file
    Then standard output is "No tracked locations."
    And the exit code is 0

  Scenario: The launched program runs the forecast command by its name
    When a person launches the program with "forecast 1" on the database file
    Then standard error has a line that says no tracked location has the id and holds "1"
    And the exit code is 1
