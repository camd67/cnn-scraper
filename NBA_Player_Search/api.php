<?php
    require_once('Player.php');
    require_once('database.php');

    $settings = parse_ini_file('settings.ini');
    $dbConn = new DatabaseConnection($settings['user'], $settings['password'], $settings['host']);

    $query;
    if(isset($_REQUEST['query'])){
        $query = $_REQUEST['query'];
    } else {
        $query = '';
    }
    $results = $dbConn->searchExactPlayer($query);

    header("Content-Type: application/json");
    if (count($results) <= 0) {
        echo $_REQUEST['callback'].'('."{'found': 'false'})";
    } else {

        echo $_REQUEST['callback'].'('."{'found': 'true', player: ".$results[0]->getAllAsJson()."})";
    }
?>
