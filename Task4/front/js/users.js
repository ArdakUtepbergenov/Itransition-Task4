let params = new URLSearchParams(window.location.search);
let currentUserId = params.get("userId") || localStorage.getItem("userId");

if (!currentUserId) {
    window.location.href = "index.html";
} else {
    localStorage.setItem("userId", currentUserId);
}

fetch(`http://localhost:5173/users?userId=${currentUserId}`, {
    method: "GET"
})
.then(response => {
    if (response.status === 401) {
        window.location.href = "index.html";
    }
    return response.json();
})
.then(data => {
    data.forEach(user => {
        document.getElementById("usersTableBody").innerHTML += `<tr><td><input type="checkbox" class="rowCheckbox" data-id="${user.id}"></td><td>${user.id}</td><td>${user.username}</td><td>${user.email}</td><td>${user.status}</td><td>${timeAgo(user.lastLogin)}</td></tr>`;
    });
    document.querySelectorAll(".rowCheckbox").forEach(checkbox => {
        checkbox.addEventListener("change", function() {
            updateToolbar();
        });
    });
})

function logout() {
    localStorage.clear();
    window.location.href = "index.html";
}

function timeAgo(dateString) {
    let now = new Date();
    let past = new Date(dateString);
    let diffInSeconds = (now - past) / 1000;
    let diffInMinutes = diffInSeconds / 60;
    let diffInHours = diffInMinutes / 60;
    let diffInDays = diffInHours / 24;
    
    if (diffInSeconds < 60) {
        return "less than a minute ago";
    } else if (diffInMinutes < 60) {
        return Math.floor(diffInMinutes) + " minutes ago";
    } else if (diffInHours < 24) {
        return Math.floor(diffInHours) + " hours ago";
    } else {
        return Math.floor(diffInDays) + " days ago";
    }
}

function toggleSelectAll(){
    let isChecked = document.getElementById("selectAll").checked;
    document.querySelectorAll(".rowCheckbox").forEach(checkbox => {
        checkbox.checked = isChecked;
        updateToolbar();
    });
}

function updateToolbar() {
    let selectedCount = document.querySelectorAll(".rowCheckbox:checked").length;
    document.getElementById("blockBtn").disabled = (selectedCount === 0);
    document.getElementById("deleteBtn").disabled = (selectedCount === 0);
    document.getElementById("unblockBtn").disabled = (selectedCount === 0);
}

function blockSelected() {
    let checkboxArray = [];
    document.querySelectorAll(".rowCheckbox").forEach(checkbox => {
        if (checkbox.checked) {
            checkboxArray.push(parseInt(checkbox.dataset.id));
        }
    });
    fetch("http://localhost:5173/api/block", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({userId: parseInt(currentUserId), ids: checkboxArray})
    })
    .then(() => location.reload());
}

function unblockSelected() {
    let checkboxArray = [];
    document.querySelectorAll(".rowCheckbox").forEach(checkbox => {
        if (checkbox.checked) {
            checkboxArray.push(parseInt(checkbox.dataset.id));
        }
    });
    fetch("http://localhost:5173/api/unblock", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({userId: parseInt(currentUserId), ids: checkboxArray})
    })
    .then(() => location.reload());
}

function deleteSelected() {
    let checkboxArray = [];
    document.querySelectorAll(".rowCheckbox").forEach(checkbox => {
        if (checkbox.checked) {
            checkboxArray.push(parseInt(checkbox.dataset.id));
        }
    });
    fetch("http://localhost:5173/api/delete", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({userId: parseInt(currentUserId), ids: checkboxArray})
    })
    .then(() => location.reload());
}

function deleteUnverified() {
    fetch("http://localhost:5173/api/deleteUnverified", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({userId: parseInt(currentUserId), ids: []})
    })
    .then(() => location.reload());
}