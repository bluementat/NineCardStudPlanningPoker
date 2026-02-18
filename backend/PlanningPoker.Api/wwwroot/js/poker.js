const connection = new signalR.HubConnectionBuilder()
    .withUrl("/planningpokerhub")
    .withAutomaticReconnect()
    .build();

connection.on("ParticipantJoined", (participant) => {
    addParticipantToList(participant);
});

connection.on("VoteSubmitted", (data) => {
    updateParticipantVotedStatus(data.participantId, true);
});

connection.on("VotesRevealed", () => {
    fetchResults();
});

connection.on("NewRoundStarted", () => {
    resetUIForNewRound();
});

connection.on("SessionEnded", () => {
    alert("The session has been ended by the host.");
    window.location.href = "/";
});

async function startConnection() {
    try {
        await connection.start();
        console.log("SignalR Connected.");
        await connection.invoke("JoinSession", sessionPin, participantId, participantName);
    } catch (err) {
        console.log(err);
        setTimeout(startConnection, 5000);
    }
}

function addParticipantToList(participant) {
    const list = document.getElementById("participantsList");
    const id = participant.participantId;
    const name = participant.name;

    if (!document.querySelector(`[data-participant-id="${id}"]`)) {
        const item = document.createElement("div");
        item.className = "participant-item";
        item.setAttribute("data-participant-id", id);
        item.innerHTML = `
            <div class="chip-avatar">${name[0].toUpperCase()}</div>
            <span class="participant-name">${name}</span>
        `;
        list.appendChild(item);
    }
}

function updateParticipantVotedStatus(id, hasVoted) {
    const item = document.querySelector(`[data-participant-id="${id}"]`);
    if (item) {
        if (hasVoted) {
            item.classList.add("has-voted");
            if (!item.querySelector(".vote-indicator")) {
                const indicator = document.createElement("span");
                indicator.className = "vote-indicator";
                indicator.innerText = "VOTED";
                item.appendChild(indicator);
            }
        } else {
            item.classList.remove("has-voted");
            const indicator = item.querySelector(".vote-indicator");
            if (indicator) indicator.remove();
        }
    }
}

async function submitVote(value, suit, color) {
    try {
        const response = await fetch(`/api/sessions/${sessionPin}/votes`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ participantId, cardValue: value })
        });
        if (response.ok) {
            document.querySelectorAll('.playing-card').forEach(c => c.classList.remove('selected'));
            const selectedCard = document.querySelector(`[data-value="${value}"]`);
            if (selectedCard) {
                selectedCard.classList.add('selected');
            }
            document.getElementById('selectedCardInfo').innerText = `You selected: ${value}`;
        }
    } catch (err) {
        console.error("Error submitting vote:", err);
    }
}

async function revealVotes() {
    try {
        await fetch(`/api/sessions/${sessionPin}/reveal`, { method: 'POST' });
    } catch (err) {
        console.error("Error revealing votes:", err);
    }
}

async function resetSession() {
    try {
        await fetch(`/api/sessions/${sessionPin}/reset`, { method: 'POST' });
    } catch (err) {
        console.error("Error resetting session:", err);
    }
}

async function endSession() {
    if (confirm("Are you sure you want to close this table?")) {
        try {
            await fetch(`/api/sessions/${sessionPin}`, { method: 'DELETE' });
        } catch (err) {
            console.error("Error ending session:", err);
        }
    }
}

async function fetchResults() {
    try {
        const response = await fetch(`/api/sessions/${sessionPin}/results`);
        const results = await response.json();
        displayResults(results);
    } catch (err) {
        console.error("Error fetching results:", err);
    }
}

function getSuitAndColor(value) {
    const suits = ["♠", "♥", "♣", "♦"];
    const values = ["0", "1", "2", "3", "5", "8", "13", "21", "∞"];
    const index = values.indexOf(value);
    if (index === -1) return { suit: "♠", color: "black" };
    const suit = suits[index % 4];
    const color = (suit === "♥" || suit === "♦") ? "red" : "black";
    return { suit, color };
}

function displayResults(results) {
    const container = document.getElementById("votesContainer");
    container.innerHTML = "";
    
    results.votes.forEach(vote => {
        const { suit, color } = getSuitAndColor(vote.cardValue);
        const item = document.createElement("div");
        item.className = "vote-item";
        item.innerHTML = `
            <div class="playing-card revealed">
                <div class="card-inner">
                    <div class="card-front ${color}">
                        <div class="card-corner top-left">
                            <span class="card-value">${vote.cardValue}</span>
                            <span class="card-suit">${suit}</span>
                        </div>
                        <div class="card-center">
                            <span class="card-suit-large">${suit}</span>
                        </div>
                        <div class="card-corner bottom-right">
                            <span class="card-value">${vote.cardValue}</span>
                            <span class="card-suit">${suit}</span>
                        </div>
                    </div>
                </div>
            </div>
            <span class="participant-name">${vote.participantName}</span>
        `;
        container.appendChild(item);
    });

    if (results.statistics) {
        document.getElementById("statAverage").innerText = results.statistics.average.toFixed(1);
        document.getElementById("statMin").innerText = results.statistics.min;
        document.getElementById("statMax").innerText = results.statistics.max;
    }

    document.getElementById("votingSection").style.display = "none";
    document.getElementById("resultsSection").style.display = "block";
}

function resetUIForNewRound() {
    document.getElementById("votingSection").style.display = "block";
    document.getElementById("resultsSection").style.display = "none";
    document.querySelectorAll('.playing-card').forEach(c => c.classList.remove('selected'));
    document.getElementById('selectedCardInfo').innerText = "Choose a card to vote";
    document.querySelectorAll('.participant-item').forEach(item => {
        item.classList.remove('has-voted');
        const indicator = item.querySelector('.vote-indicator');
        if (indicator) indicator.remove();
    });
}

// Modal and Copy functions
function closePinModal() {
    document.getElementById("pinModal").style.display = "none";
}

function copyPin() {
    navigator.clipboard.writeText(sessionPin).then(() => {
        showToast("PIN Copied to Clipboard!");
    });
}

function showToast(message) {
    const toast = document.createElement("div");
    toast.className = "copy-feedback";
    toast.innerText = message;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 2000);
}

// Start SignalR
startConnection();

// Show modal if it's a new session
if (typeof isNewSession !== 'undefined' && isNewSession) {
    document.getElementById("pinModal").style.display = "flex";
}

// If already revealed on load (though our PageModel defaults to false for now)
if (typeof initialIsRevealed !== 'undefined' && initialIsRevealed) {
    fetchResults();
}
