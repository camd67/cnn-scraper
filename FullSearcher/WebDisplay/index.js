"use strict";
$(function () {
    var input = $("#search");
    var results = $(".results");
    var instaResults = $(".insta-results");
    var staticResults = $(".staticResults");
    staticResults.hide();

    input.on("paste keyup", function () {
        var query = input.val();
        if (query == null || query === "") {
            instaResults.empty();
            return;
        }
        // get data from wiki
        $.ajax({
            type: "POST",
            url: "SearchEngine.asmx/Search",
            data: "{ q: \"" + query + "\" }",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                instaResults.empty();
                if (response.d == null || response.d.length <= 0) {
                    instaResults.append($("<li>")
                        .text("No results found!"));
                } else {
                    response.d.forEach(function (word) {
                        instaResults.append($("<li>")
                            .text(word)
                            .click(function () {
                                input.val(word)
                            }));
                    });
                }
            },
            error: function (response) {
                console.log("Error searching for title");
                console.error(response);
            }
        });
    });

    $(".sendSearch").click(function () {
        var query = input.val();
        results.empty();
        instaResults.empty();
        staticResults.hide();
        if (query == null || query === "") {
            results.append("No results found");
            return;
        }
        // get data from EC2
        $.ajax({
            crossDomain: true,
            url: "http://ec2-52-53-199-168.us-west-1.compute.amazonaws.com/api.php",
            contentType: "applicaton/json;charset:utf-8",
            dataType: "jsonp",
            data: { query: query },
            success: function (data) {
                if (data.found == "true") {
                    insertPlayerIntoPage(data.player);
                    staticResults.show();
                }
            },
            error: function (data) {
                console.log(data);
            }
        });
        // get data from azure table
        $.ajax({
            type: "POST",
            url: "SearchEngine.asmx/SearchFromScraped",
            data: "{ q: \"" + query + "\" }",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                for(var index in response.d) {
                    var toAdd = $("<li/>");
                    toAdd.append($("<p/>").text(response.d[index].Title));
                    toAdd.append($("<p/>").text(convertDateToText(response.d[index].Date)));
                    toAdd.append($("<a/>", {
                        text: response.d[index].URL,
                        href: response.d[index].URL,
                        target: "_blank"
                    }));
                    results.append(toAdd);
                }
            },
            error: function (response) {
                console.log("Error searching for title");
                console.error(response);
            }
        });
    });
    function convertDateToText(date) {
        var foundTime = parseInt(date.substring(date.indexOf("(") + 1, date.indexOf(")")));
        return new Date(foundTime).toDateString();
    }

    function insertPlayerIntoPage(player) {
        var card = $(".card");
        card.find(".card-header .left .larger").text(player.name + " - " + player.team);
        card.find(".card-header .right .gp").text("Games played: " + player.gPlayed);
        card.find(".card-header .right .smaller").text("Minutes: " + player.minPlayed + " - PPG: " + player.pointsPerGame);
        var table1 = card.find(".card-content");
        // field goals
        table1.find(".fgm").text(player.fGoalsMade);
        table1.find(".fga").text(player.fGoalsAttempted);
        table1.find(".fgp").text(player.fGoalsPercent);
        // free throws
        table1.find(".ftm").text(player.freeThrowsMade);
        table1.find(".fta").text(player.freeThrowAttempts);
        table1.find(".ftp").text(player.freeThrowPct);
        // three pointers
        table1.find(".tpm").text(player.threePointsMade);
        table1.find(".tpa").text(player.threePointAttempts);
        table1.find(".tpp").text(player.threePointPct);
        var table2 = card.find(".misc-stats");
        table2.find(".assists").text(player.assists);
        table2.find(".turnovers").text(player.turnovers);
        table2.find(".steals").text(player.steals);
        table2.find(".blocks").text(player.blocks);
        table2.find(".pFouls").text(player.personalFouls);
        var table3 = card.find(".rebound-stats");
        table3.find(".rOff").text(player.reboundsOffensive);
        table3.find(".rDef").text(player.reboundsDefensive);
        table3.find(".rTot").text(player.reboundsTotal);
    }
});