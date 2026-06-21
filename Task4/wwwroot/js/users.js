fetch("/users", {
    method: "GET",
    credentials: "include"
})
.then(response => {
    if (response.status === 401) {
        window.location.href = "index.html";
    }
    return response.json();
})
.then(data => {
    document.getElementById("mainContent").style.display = "block";
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
    fetch("/api/logout", {
        method: "POST",
        credentials: "include",
    })
    .then(() => window.location.href = "index.html");
}

function timeAgo(dateString) {
    let now = new Date();
    let past = new Date(dateString + "Z");
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
    fetch("/api/block", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    credentials: "include",
    body: JSON.stringify(checkboxArray)
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
    fetch("/api/unblock", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    credentials: "include",
    body: JSON.stringify(checkboxArray)
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
    fetch("/api/delete", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    credentials: "include",
    body: JSON.stringify(checkboxArray)
    })
    .then(() => location.reload());
}

function deleteUnverified() {
    fetch("/api/deleteUnverified", {
        method: "POST",
        credentials: "include",
    })
    .then(() => location.reload());
}