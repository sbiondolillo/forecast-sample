Feature: Seeing the current weather at a tracked location

  A person asks for the current weather at a tracked location by its id. The
  program reads the location from the database file, which the option
  --database names, and asks the forecast service of Open-Meteo for the
  current weather at its coordinates.

  Background:
    Given a database file that does not exist yet

  Scenario: The program asks the forecast service for the coordinates of the location
    Given the geocoding service answers with one result named "Kathmandu" at latitude 27.70169 and longitude 85.3206
    And a person ran the program with "add Kathmandu" on the database file
    And the forecast service answers with temperature 20.4 "°C" and wind speed 1.9 "km/h"
    When a person runs the program with "forecast 1" on the database file
    Then the program sent the request "GET https://api.open-meteo.com/v1/forecast?latitude=27.70169&longitude=85.3206&current=temperature_2m,wind_speed_10m"

  Scenario: The current weather at a tracked location is printed
    Given the geocoding service answers with one result named "Kathmandu" at latitude 27.70169 and longitude 85.3206
    And a person ran the program with "add Kathmandu" on the database file
    And the forecast service answers with temperature 20.4 "°C" and wind speed 1.9 "km/h"
    When a person runs the program with "forecast 1" on the database file
    Then standard output is "Kathmandu (27.70169, 85.3206): 20.4 °C, wind 1.9 km/h"
    And the exit code is 0

  Scenario: The id picks the location among several
    Given the geocoding service answers with one result named "Kathmandu" at latitude 27.70169 and longitude 85.3206
    And a person ran the program with "add Kathmandu" on the database file
    And the geocoding service answers with one result named "Vaduz" at latitude 47.14151 and longitude 9.52154
    And a person ran the program with "add Vaduz" on the database file
    And the forecast service answers with temperature 20.1 "°C" and wind speed 6.9 "km/h"
    When a person runs the program with "forecast 2" on the database file
    Then the program sent the request "GET https://api.open-meteo.com/v1/forecast?latitude=47.14151&longitude=9.52154&current=temperature_2m,wind_speed_10m"
    And standard output is "Vaduz (47.14151, 9.52154): 20.1 °C, wind 6.9 km/h"
    And the exit code is 0

  Scenario: An id that no tracked location has is refused
    Given the geocoding service answers with one result named "Kathmandu" at latitude 27.70169 and longitude 85.3206
    And a person ran the program with "add Kathmandu" on the database file
    And the geocoding service answers with one result named "Vaduz" at latitude 47.14151 and longitude 9.52154
    And a person ran the program with "add Vaduz" on the database file
    And the forecast service answers with temperature 20.4 "°C" and wind speed 1.9 "km/h"
    When a person runs the program with "forecast 3" on the database file
    Then the program sent no request to the forecast service
    And standard error has a line that says no tracked location has the id and holds "3"
    And standard output is empty
    And the exit code is 1

  Scenario: No database file means no tracked location
    Given the forecast service answers with temperature 20.4 "°C" and wind speed 1.9 "km/h"
    When a person runs the program with "forecast 1" on the database file
    Then the program sent no request
    And standard error has a line that says no tracked location has the id and holds "1"
    And standard output is empty
    And the exit code is 1

  Scenario: A request that gets no answer is refused
    Given the geocoding service answers with one result named "Kathmandu" at latitude 27.70169 and longitude 85.3206
    And a person ran the program with "add Kathmandu" on the database file
    And the forecast service gives no answer
    When a person runs the program with "forecast 1" on the database file
    Then standard error has a line that says the forecast service gave no answer and holds "Kathmandu"
    And standard output is empty
    And the exit code is 1

  Scenario: A request that the service refuses is refused
    Given the geocoding service answers with one result named "Kathmandu" at latitude 27.70169 and longitude 85.3206
    And a person ran the program with "add Kathmandu" on the database file
    And the forecast service answers with the status 400 and the reason "Latitude must be in range of -90 to 90°. Given: 99.0."
    When a person runs the program with "forecast 1" on the database file
    Then standard error has a line that says the forecast service refused the request and holds "Latitude must be in range of -90 to 90°. Given: 99.0."
    And standard output is empty
    And the exit code is 1

  Scenario: A missing id sends no request
    Given the forecast service answers with temperature 20.4 "°C" and wind speed 1.9 "km/h"
    When a person runs the program with "forecast" on the database file
    Then the program sent no request
    And standard error is not empty
    And the exit code is not 0

  Scenario: The forecast command lists the database option and its default
    When a person runs the program with "forecast --help"
    Then standard output holds the option "--database" and its default value "forecast.db"
