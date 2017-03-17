<?php
    class Player {
        private $Name;
        private $Team;
        private $GamesPlayed;
        private $MinutesPlayed;
        private $FieldGoalsMade;
        private $FieldGoalAttempts;
        private $FieldGoalPct;
        private $ThreePointsMade;
        private $ThreePointAttempts;
        private $ThreePointPct;
        private $FreeThrowsMade;
        private $FreeThrowAttempts;
        private $FreeThrowPct;
        private $ReboundsOffensive;
        private $ReboundsDefensive;
        private $ReboundsTotal;
        private $Assists;
        private $Turnovers;
        private $Steals;
        private $Blocks;
        private $PersonalFouls;
        private $PointsPerGame;

        function __construct($n, $t, $gp, $mp, $fgm, $fga, $fgp, $tpm, $tpa, $tpp, $ftm, $fta, $ftp, $ro, $rd, $rt, $a, $turn, $s, $b, $pf, $ppg){
            $this->Name = $n;
            $this->Team = $t;
            $this->GamesPlayed = $gp;
            $this->MinutesPlayed = $mp;
            $this->FieldGoalsMade = $fgm;
            $this->FieldGoalAttempts = $fga;
            $this->FieldGoalPct = $fgp;
            $this->ThreePointsMade = $tpm;
            $this->ThreePointAttempts = $tpa;
            $this->ThreePointPct = $tpp;
            $this->FreeThrowsMade = $ftm;
            $this->FreeThrowAttempts = $fta;
            $this->FreeThrowPct = $ftp;
            $this->ReboundsOffensive = $ro;
            $this->ReboundsDefensive = $rd;
            $this->ReboundsTotal = $rt;
            $this->Assists = $a;
            $this->Turnovers = $turn;
            $this->Steals = $s;
            $this->Blocks = $b;
            $this->PersonalFouls = $pf;
            $this->PointsPerGame = $ppg;
        }

        public function getAllAsJson(){
            return '{'
                    ."'name':'".$this->getName()."',"
                    ."'team':'".$this->getTeam()."',"
                    ."'gPlayed':'".$this->getGamesPlayed()."',"
                    ."'minPlayed':'".$this->getMinutesPlayed()."',"
                    ."'fGoalsMade':'".$this->getFieldGoalsMade()."',"
                    ."'fGoalsAttempted':'".$this->getFieldGoalAttempts()."',"
                    ."'fGoalsPercent':'".$this->getFieldGoalPct()."',"
                    ."'threePointsMade':'".$this->getThreePointsMade()."',"
                    ."'threePointAttempts':'".$this->getThreePointAttempts()."',"
                    ."'threePointPct':'".$this->getThreePointPct()."',"
                    ."'freeThrowsMade':'".$this->getFreeThrowsMade()."',"
                    ."'freeThrowAttempts':'".$this->getFreeThrowAttempts()."',"
                    ."'freeThrowPct':'".$this->getFreeThrowPct()."',"
                    ."'reboundsOffensive':'".$this->getReboundsOffensive()."',"
                    ."'reboundsDefensive':'".$this->getReboundsDefensive()."',"
                    ."'reboundsTotal':'".$this->getReboundsTotal()."',"
                    ."'assists':'".$this->getAssists()."',"
                    ."'turnovers':'".$this->getTurnovers()."',"
                    ."'steals':'".$this->getSteals()."',"
                    ."'blocks':'".$this->getBlocks()."',"
                    ."'personalFouls':'".$this->getPersonalFouls()."',"
                    ."'pointsPerGame':'".$this->getPointsPerGame()."'}";
        }

        public function getName() { return $this->Name; }
        public function getTeam() { return $this->Team; }
        public function getGamesPlayed() { return $this->GamesPlayed; }
        public function getMinutesPlayed() { return $this->MinutesPlayed; }
        public function getFieldGoalsMade() { return $this->FieldGoalsMade; }
        public function getFieldGoalAttempts() { return $this->FieldGoalAttempts; }
        public function getFieldGoalPct() { return $this->FieldGoalPct; }
        public function getThreePointsMade() { return $this->ThreePointsMade; }
        public function getThreePointAttempts() { return $this->ThreePointAttempts; }
        public function getThreePointPct() { return $this->ThreePointPct; }
        public function getFreeThrowsMade() { return $this->FreeThrowsMade; }
        public function getFreeThrowAttempts() { return $this->FreeThrowAttempts; }
        public function getFreeThrowPct() { return $this->FreeThrowPct; }
        public function getReboundsOffensive() { return $this->ReboundsOffensive; }
        public function getReboundsDefensive() { return $this->ReboundsDefensive; }
        public function getReboundsTotal() { return $this->ReboundsTotal; }
        public function getAssists() { return $this->Assists; }
        public function getTurnovers() { return $this->Turnovers; }
        public function getSteals() { return $this->Steals; }
        public function getBlocks() { return $this->Blocks; }
        public function getPersonalFouls() { return $this->PersonalFouls; }
        public function getPointsPerGame() { return $this->PointsPerGame; }
    }
?>
