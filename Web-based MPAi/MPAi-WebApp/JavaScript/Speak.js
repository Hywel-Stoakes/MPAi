﻿var blob = null;

var player = videojs("myAudio",
		{
		    controls: true,
		    width: 800,
		    height: 200,
		    plugins: {
		        wavesurfer: {
		            src: "live",
		            waveColor: "#000000",
		            progressColor: "#AB0F12",
		            debug: true,
		            cursorWidth: 1,
		            msDisplayMax: 20,
		            hideScrollbar: true
		        },
		        record: {
		            audio: true,
		            video: false,
		            maxLength: 20,
		            debug: true,
		            audioEngine: "recorder.js"
		        }
		    },
		    controlBar: {
		        fullscreenToggle: false
		    }
		});

player.on('ready', function () {
    console.log('videojs is ready');
    player.recorder.getDevice();
});

// error handling
player.on('deviceError', function () {
    console.log('device error:', player.deviceErrorCode);
});
player.on('error', function (error) {
    console.log('error:', error);
});

// user clicked the record button and started recording
player.on('startRecord', function () {
    console.log('started recording!');
});

// user completed recording and stream is available
player.on('finishRecord', function () {
    // the blob object contains the recorded data that
    // can be downloaded by the user, stored on server etc.
    console.log('finished recording: ', player.recordedData);

    blob = player.recordedData;

    $('#analyse').show();
});

//$(window).resize(function () {
//    player.wavesurfer.drawer.containerWidth = player.wavesurfer.drawer.container.clientWidth;
//    player.wavesurfer.drawBuffer();
//});

$('document').ready(function (e) {
    $('#record').collapse({ toggle: false });
    $('#searchErrorMessage').collapse({ toggle: false });
});

// initialization
window.onbeforeunload = function () {
    document.getElementById(maoriWord.id).value = "";
};

$('#maoriWord').keypress(function (event) {
    var keycode = (event.keyCode ? event.keyCode : event.which);
    if (keycode == '13') {
        getTarget();
        return false;
    }
});

var expectedWord = null;

// Button 'search' action
$('#search').click(getTarget);

$('#maoriWord').on('input', function () {
    $('#searchErrorMessage').collapse('hide');
});

function getTarget() {
    player.recorder.reset();

    if (wordIsEmpty()) {
        searchErrorMessage.innerText = "You must choose a Māori word";
        $('#searchErrorMessage').collapse('show');
        maoriWord.value = "";
        recordMessage.innerText = "";
        expectedWord = null;
        $('#record').collapse('hide');
        $('#analyse').hide();
        return;
    }

    var wordName = getApprovedWord();

    if (wordName === "none") {
        searchErrorMessage.innerText = "Sorry, '" + maoriWord.value + "' is not recognised\nClick on the search bar to see a list of supported words";
        $('#searchErrorMessage').collapse('show');
        maoriWord.value = "";
        recordMessage.innerText = "";
        expectedWord = null;
        $('#record').collapse('hide');
        $('#analyse').hide();
        return;
    }

    $('#searchErrorMessage').collapse('hide');
    recordMessage.innerText = "Please record your pronounciation of the word '" + wordName + "' below";
    expectedWord = wordName.replace(/ /g, "_");
    $('#record').collapse('show');
    $('#analyse').hide();

    console.log("Target: " + expectedWord);
}

$("#analyse").click(function () {
    if (blob) {
        analyse(blob);
    } else {
        console.log("Recording not found :(");

        showModal("white", ["<h4>Sorry, your recording was not found :(</h4>"]);
    }
    reset();
});

function reset() {
    $('#analyse').hide();
    blob = null;
}

// analyse function
function analyse(blob) {
    console.log("Maori word: " + expectedWord);
    upload(blob, callBack);
}

function callBack(response) {
    console.log("Response: " + response);
    
    if (!response || response === "nothing") {
        showModal("white", ["<h4>Sorry, your pronunciation cannot be recognised</h4>"]);
    } else {
        var data = JSON.parse(response);
        if (data.result === "nothing") {
            showModal("white", ["<h4>Sorry, your pronunciation cannot be recognised</h4>"]);
        } else {
            processResult(data);
        }
    }
}

function processResult(data) {
    var categories = {
        BELOW_AVG: "red", ABOVE_AVG: "orange", EXCELLENT: "yellow", PERFECT: "green", UNDEFINED: "white"
    };

    var score = data.score;
    var score = 100;
    var result = data.result.replace(/_/g, ' ');
    var category;

    if (score >= 0 && score < 50) {
        category = categories.BELOW_AVG;
    } else if (score >= 0 && score < 80) {
        category = categories.ABOVE_AVG;
    } else if (score >= 0 && score < 100) {
        category = categories.EXCELLENT;
    } else if (score === 100) {
        category = categories.PERFECT;
    } else {
        category = categories.UNDEFINED;
    }

    var bodyElements;

    if (category === categories.UNDEFINED) {
        bodyElements = ["<h4>Sorry, your pronunciation cannot be recognised</h4>"];
    } else if (category === categories.PERFECT) {
        var resultText = "<h2>Ka Pai!</h2>";
        var introText = "<h3>Your score is</h3>";
        var scoreText = "<h1>100%</h1>";

        bodyElements = [resultText, introText, scoreText];
    } else {
        var introText = "<h3>Your score is</h3>";
        var scoreText = "<h1>" + Math.floor(score) + "%</h1>";
        var resultText = "<h4>The word you pronounced is recognised as: \"" + result + "\"</h4>";

        bodyElements = [introText, scoreText, resultText];
    }
    
    showModal(category, bodyElements);
}

function showModal(colour, bodyElements) {

    $("#score-body").empty();
    for (i = 0; i < bodyElements.length; i++) {
        console.log(bodyElements[i]);
        $("#score-body").append(bodyElements[i]);
    }

    $("#score-report").modal();
    $("#score-header").css("background-color", colour);
}

// upload audio file to server
function upload(blob, callBack) {
    var time = new Date().getTime().toString();
    var currentdate = new Date();
    var datetime = currentdate.getDate() + "-"
					+ (currentdate.getMonth() + 1) + "-"
					+ currentdate.getFullYear() + "@"
					+ currentdate.getHours() + "-"
					+ currentdate.getMinutes() + "-"
					+ currentdate.getSeconds() + "@";
    var fileName = datetime + time + '.wav';

    var formData = new FormData();
    formData.append('fileName', fileName);
    formData.append('blob', blob);
    formData.append('target', expectedWord);

    xhr('Save.aspx', formData, callBack);
}