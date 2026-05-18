const nav = document.querySelector('.topnav')
fetch('topnav.html')
.then(res=>res.text())
.then(data=>{
    nav.innerHTML=data
})

function toggleMenu() {
    var x = document.getElementById("myTopnav");
    if (x.className === "topnav") {
    x.className += " responsive";
    } else {
    x.className = "topnav";
    }
}

function stickyNav(){        
    var navbar = document.getElementById("myTopnav");
    var sticky = navbar.offsetTop;
    
    if (window.pageYOffset >= sticky) {
        navbar.classList.add("sticky")
    } else {
        navbar.classList.remove("sticky");
    }
}