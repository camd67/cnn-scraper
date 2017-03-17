$(function () {
    var dashboardFile = "Dashboard.asmx";
    var workerTable = $(".dashboard-worker-table > tbody");
    var urlCount = $(".url-count");
    var queueSize = $(".queue-size");
    var tableSize = $(".table-size");
    var trieSize = $(".trie-size");
    var lastTrie = $(".last-trie");
    var lastTenUrls = $(".last-ten-url");
    var errorList = $(".error-list");
    var urlInput = $("#pageSearch");
    var urlOutput = $(".search-results");

    $(".stop-button").click(function () {
        popup("Stop command sent. This may take some time to complete");
        $.ajax({
            type: "POST",
            url: dashboardFile + "/StopCrawlers",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                console.log(response);
                popup(response.d);
            },
            error: function (error) {
                console.error(error);
            }
        });
    });

    $(".crawl-cnn-button").click(function () {
        $.ajax({
            type: "POST",
            data: "{ url: \"http://www.cnn.com/robots.txt\" }",
            url: dashboardFile + "/StartCrawlersUrl",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                popup(response.d);
                console.log(response.d);
            },
            error: function (error) {
                console.error(error);
            }
        });
    });
    $(".crawl-br-button").click(function () {
        $.ajax({
            type: "POST",
            url: dashboardFile + "/StartCrawlersUrl",
            data: "{ url: \"http://bleacherreport.com/robots.txt\" }",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                console.log(response);
                popup(response.d);
            },
            error: function (error) {
                console.error(error);
            }
        });
    });
    $(".crawl-both-button").click(function () {
        $.ajax({
            type: "POST",
            url: dashboardFile + "/StartWorkers",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                console.log(response);
                popup(response.d);
            },
            error: function (error) {
                console.error(error);
            }
        });
    });
    $(".resume-button").click(function () {
        $.ajax({
            type: "POST",
            url: dashboardFile + "/ResumeWorkers",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                console.log(response);
                popup(response.d);
            },
            error: function (error) {
                console.error(error);
            }
        });
    });
    $(".pause-button").click(function () {
        $.ajax({
            type: "POST",
            url: dashboardFile + "/PauseWorkers",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                console.log(response);
                popup(response.d);
            },
            error: function (error) {
                console.error(error);
            }
        });
    });

    $(".url-search").click(function () {
        $.ajax({
            type: "POST",
            url: dashboardFile + "/GetPageInformation",
            data: "{ url: \""+urlInput.val()+"\" }",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                urlOutput.text(response.d);
            },
            error: function (error) {
                console.error(error);
                urlOutput.text("Couldn't find that url...");
            }
        });
    });

    // for auto-update set this in a window timeout interval
    function updateAll() {
        reloadWorkerStatus();
        reloadErrorList();
        reloadTotals();
        reloadLastTen();
        reloadTrieStats();
    }
    updateAll();
    setInterval(updateAll, 3000);

    function popup(text) {
        var pop = $("<div class=\"alert alert-info\" role=\"alert\">" + text + "</div>");
        pop.fadeTo(2000, 500).slideUp(500, function () {
            pop.slideUp(500);
        });
        $("body").prepend(pop);
    }

    function reloadTrieStats() {
        $.ajax({
            type: "POST",
            url: dashboardFile + "/GetTrieStats",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (response) {
                trieSize.text("Size: " + response.d.TitleCount);
                lastTrie.text("Last inserted word: " + response.d.LastTitle);
            },
            error: function (response) {
                console.error(response)
            }
        });
    }

    function reloadLastTen() {
        $.ajax({
            type: "POST",
            url: dashboardFile + "/GetRecentUrls",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: onLastTenSuccess,
            error: onLastTenError
        });
        function onLastTenSuccess(response) {
            lastTenUrls.find("li").remove();
            for (var i = 0; i < response.d.length; i++) {
                lastTenUrls.append($("<li>").text(response.d[i]))
            }
        }
        function onLastTenError(error) {
            console.error(error);
        }
    }

    function reloadTotals() {
        $.ajax({
            type: "POST",
            url: dashboardFile + "/GetLengthsAndTotals",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: onTotalSuccess,
            error: onTotalError
        });
        function onTotalSuccess(response) {
            urlCount.text(response.d[0]);
            queueSize.text(response.d[1]);
            tableSize.text(response.d[2]);
        }
        function onTotalError(error) {
            console.error(error);
        }
    }

    function reloadErrorList() {
        $.ajax({
            type: "POST",
            url: dashboardFile + "/GetAllErrors",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: onErrorListSuccess,
            error: onErrorListError
        });
        function onErrorListSuccess(response) {
            errorList.find("li").remove();
            for (var i = 0; i < response.d.length; i++) {
                errorList.append($("<li>").text(response.d[i].Error + " on page: " + response.d[i].URL))
            }
        }
        function onErrorListError(error) {
            console.error(error);
        }
    }

    function reloadWorkerStatus() {
        $.ajax({
            type: "POST",
            url: dashboardFile + "/GetAllWorkers",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: gotAllWorkers,
            error: gotAllWorkersError
        });
        function gotAllWorkers(response) {
            workerTable.find("tr").remove();
            for (var i = 0; i < response.d.length; i++) {
                document.title = "Dashboard - "+ response.d[i].State;
                var newRow = $("<tr>");
                newRow.append($("<td>").text(response.d[i].Name));
                newRow.append($("<td>").text(response.d[i].State));
                newRow.append($("<td>").text(Math.round(response.d[i].CpuPercent * 100) / 100));
                newRow.append($("<td>").text(response.d[i].RamOpen));
                workerTable.prepend(newRow);
            }
        }
        function gotAllWorkersError(error) {
            console.error(error);
        }
    }
});