<?php
    class DatabaseConnection {
        private $user;
        private $pass;
        private $conn;
        private $host;
        private $database;

        function __construct($user, $pass, $host){
            $this->user = $user;
            $this->pass = $pass;
            $this->host = $host;
            $this->database = 'nba_stats';
            $this->conn = new PDO('mysql:host='.$this->host.';dbname='.$this->database, $user, $pass);
        }

        public function searchExactPlayer($query){
            $dbStatement = $this->conn->prepare("SELECT * FROM player WHERE Name = :nameQuery");
            $dbStatement->execute(array('nameQuery' => $query));
            $dbStatement->setFetchMode(PDO::FETCH_ASSOC);
            return $this->parseQueryResult($dbStatement);
        }

        public function searchPlayers($query){
            $dbStatement = $this->conn->prepare("SELECT * FROM player WHERE Name LIKE :nameQuery");
            $search = '%'.$query.'%';
            $dbStatement->execute(array('nameQuery' => $search));
            $dbStatement->setFetchMode(PDO::FETCH_ASSOC);
            return $this->parseQueryResult($dbStatement);
        }

        private function parseQueryResult($dbStatement){
            $players = array();
            foreach ($dbStatement as $row) {
                $players[] = new Player($row['Name'], $row['Team'], $row['GamesPlayed'], $row['MinutesPlayed'],
                        $row['FieldGoalsMade'], $row['FieldGoalAttempts'], $row['FieldGoalPct'],
                        $row['ThreePointsMade'], $row['ThreePointAttempts'], $row['ThreePointPct'],
                        $row['FreeThrowsMade'], $row['FreeThrowAttempts'], $row['FreeThrowPct'],
                        $row['ReboundsOffensive'], $row['ReboundsDefensive'], $row['ReboundsTotal'],
                        $row['Assists'], $row['Turnovers'], $row['Steals'], $row['Blocks'], $row['PersonalFouls'], $row['PointsPerGame']);
            }
            return $players;
        }
    }
?>
