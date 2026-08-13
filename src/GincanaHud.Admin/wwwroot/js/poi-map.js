window.gincanaPoiMap = (() => {
  const maps = new Map();

  function ensure(mapId) {
    return maps.get(mapId);
  }

  return {
    init: function (mapId, dotNetRef, lat, lon, zoom) {
      const el = document.getElementById(mapId);
      if (!el) return;

      const existing = maps.get(mapId);
      if (existing) {
        existing.map.remove();
        maps.delete(mapId);
      }

      const map = L.map(mapId).setView([lat, lon], zoom);
      L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: "&copy; OpenStreetMap"
      }).addTo(map);

      const poiLayer = L.layerGroup().addTo(map);
      let pickMarker = null;

      map.on("click", (e) => {
        if (pickMarker) pickMarker.setLatLng(e.latlng);
        else pickMarker = L.marker(e.latlng).addTo(map);
        dotNetRef.invokeMethodAsync("OnMapClicked", e.latlng.lat, e.latlng.lng);
      });

      maps.set(mapId, { map, poiLayer, pickMarker: () => pickMarker, setPickMarker: (m) => { pickMarker = m; }, dotNetRef });

      // Leaflet needs a tick after layout
      setTimeout(() => map.invalidateSize(), 80);
    },

    setPick: function (mapId, lat, lon) {
      const state = ensure(mapId);
      if (!state) return;
      const ll = L.latLng(lat, lon);
      let marker = state.pickMarker();
      if (marker) marker.setLatLng(ll);
      else state.setPickMarker(L.marker(ll).addTo(state.map));
    },

    flyTo: function (mapId, lat, lon, zoom) {
      const state = ensure(mapId);
      if (!state) return;
      state.map.flyTo([lat, lon], zoom ?? 14);
    },

    setPois: function (mapId, pois) {
      const state = ensure(mapId);
      if (!state) return;
      state.poiLayer.clearLayers();
      if (!pois || !pois.length) return;

      const bounds = [];
      for (const p of pois) {
        const circle = L.circle([p.lat, p.lon], {
          radius: p.radiusMeters || 25,
          color: "#7CFFB2",
          weight: 2,
          fillOpacity: 0.15
        }).bindPopup(`<strong>${escapeHtml(p.name)}</strong><br/>#${p.order} · ${p.points} pts`);
        state.poiLayer.addLayer(circle);
        bounds.push([p.lat, p.lon]);
      }

      if (bounds.length === 1) state.map.setView(bounds[0], 16);
      else if (bounds.length > 1) state.map.fitBounds(bounds, { padding: [40, 40] });
    },

    invalidate: function (mapId) {
      const state = ensure(mapId);
      if (state) setTimeout(() => state.map.invalidateSize(), 50);
    },

    dispose: function (mapId) {
      const state = ensure(mapId);
      if (!state) return;
      state.map.remove();
      maps.delete(mapId);
    }
  };

  function escapeHtml(s) {
    return String(s ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;");
  }
})();
