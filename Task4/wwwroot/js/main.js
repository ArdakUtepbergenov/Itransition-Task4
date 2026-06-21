function login(event){
    event.preventDefault();
    username = document.getElementById("username").value;
    password = document.getElementById("password").value;

    fetch("/api/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ username: username, password: password })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            window.location.href = "users.html";
        } else {
            alert("Неверный логин или пароль");
        }
    })
}

function register(event) {
    event.preventDefault();
    username = document.getElementById("username").value;
    password = document.getElementById("password").value;
    confirmPassword = document.getElementById("confirmPassword").value;
    email = document.getElementById("email").value;

    fetch("/api/register", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ username: username, password: password, confirmPassword: confirmPassword, email: email})
    })
    .then(response => response.json())
    .then(data => {
        if(data.success){
            window.location.href = "users.html";
        } else {
            alert(data.message);
        }
    })
}
