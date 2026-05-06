let map;
let places = [];
let tempLatLng = null;

window.onload = function () {
    initMap();
    loadPlaces(); // 🔥 QUAN TRỌNG
};

    const form = document.querySelector("form");

    form.addEventListener("submit", async function (e) {
        e.preventDefault();

        const name = document.querySelector("[name='name']").value;
        const address = document.querySelector("[name='address']").value;

        if (!tempLatLng) {
            alert("Click lên bản đồ trước!");
            return;
        }

        // 🔥 GỌI API LƯU DB
        const res = await fetch("/api/restaurants", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                name: name,
                address: address,
                lat: tempLatLng.lat,
                lng: tempLatLng.lng
            })
        });

        const newPlace = await res.json();

        // thêm marker lên map
        addMarker(newPlace);

        document.getElementById('addModal').classList.remove('open');
        form.reset();
    });

function initMap() {
    map = L.map('map').setView([10.762622, 106.660172], 14);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap'
    }).addTo(map);

    // Click map để chọn vị trí
    map.on('click', function (e) {
        tempLatLng = e.latlng;
        document.getElementById('addModal').classList.add('open');
    });

    // đóng modal khi click nền
    document.getElementById('addModal').addEventListener('click', function (e) {
        if (e.target === this) this.classList.remove('open');
    });
}

function renderList() {
    const list = document.getElementById("placeList");
    list.innerHTML = "<h3>📍 Địa điểm</h3>";

    places.forEach((p) => {
        const div = document.createElement("div");

        div.style.padding = "10px";
        div.style.marginBottom = "8px";
        div.style.background = "#1c2030";
        div.style.borderRadius = "8px";
        div.style.cursor = "pointer";

        div.innerHTML = `
            <b>${p.name}</b><br/>
            <small>${p.address}</small>
        `;

        div.onclick = () => {
            map.setView([p.lat, p.lng], 16);
            p.marker.openPopup();
        };

        list.appendChild(div);
    });
    }
    async function loadPlaces() {
        const res = await fetch("/api/restaurants");
        const data = await res.json();

        data.forEach(p => addMarker(p));
}
function addMarker(p) {
    if (!p.lat || !p.lng) return;

    const marker = L.marker([p.lat, p.lng]).addTo(map);

    marker.bindPopup(`
        <b>${p.name}</b><br/>
        ${p.address}
    `);

    marker.on('click', () => {
        marker.openPopup();
    });

    addToList(p, marker);
}