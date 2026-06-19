function login(event){
    event.preventDefault();

    username = document.getElementById("username").value;
    password = document.getElementById("password").value;

    fetch("http://localhost:5173/api/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username: username, password: password })
    })
    .then(response => response.json())
    .then(data => {
    if (data.success) {
        localStorage.setItem("userId", data.userId);
        window.location.href = `users.html?userId=${data.userId}`;
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
    email  = document.getElementById("email").value;

    fetch("http://localhost:5173/api/register", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username: username, password: password, confirmPassword: confirmPassword, email: email})
    })
    .then(response => response.json())
    .then(data => {
        if(data.success){
    localStorage.setItem("userId", data.userId);
    window.location.href = `users.html?userId=${data.userId}`;

        } else {
            alert(data.message);
        }
    })
}


