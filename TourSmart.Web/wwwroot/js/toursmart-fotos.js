// TourSmart RD: cargas de fotografías desde el explorador.
(() => {
  const boton = document.querySelector("[data-foto-archivo]");
  const campo = document.querySelector("[data-foto-url]");
  const estado = document.querySelector("[data-foto-estado]");
  if (!boton || !campo || !estado) return;
  const form = campo.closest("form");
  if (!form) return;
  let ocupado = false;

  boton.addEventListener("change", async () => {
    const foto = boton.files && boton.files[0];
    if (!foto) return;
    if (ocupado) return;
    const formatos = ["image/jpeg", "image/png", "image/webp"];
    if (!formatos.includes(foto.type) || foto.size > 5 * 1024 * 1024 || !foto.size) {
      estado.textContent = "Solo JPG, PNG o WebP de hasta 5 MB.";
      estado.className = "small d-block mt-2 text-danger";
      boton.value = "";
      return;
    }
    const csrf = form.querySelector('input[name="__RequestVerificationToken"]');
    if (!csrf) {
      estado.textContent = "No se pudo verificar el formulario.";
      return;
    }
    ocupado = true;
    const submits = [...form.querySelectorAll('button[type="submit"]')];
    submits.forEach(x => x.disabled = true);
    estado.className = "small d-block mt-2 text-secondary";
    estado.textContent = "Subiendo fotografía...";
    const data = new FormData();
    data.append("archivo", foto);
    data.append("__RequestVerificationToken", csrf.value);
    try {
      const response = await fetch("/panel/imagenes/subir", {
        method: "POST", body: data, credentials: "same-origin",
        headers: { "Accept": "application/json" }
      });
      const result = await response.json().catch(() => ({}));
      if (!response.ok || !result.url) throw new Error(result.error || "No fue posible subir la foto.");
      campo.value = result.url;
      campo.dispatchEvent(new Event("change", { bubbles: true }));
      estado.className = "small d-block mt-2 text-success";
      estado.textContent = "Fotografía subida. Guarda el formulario para asociarla al registro.";
    } catch (error) {
      estado.className = "small d-block mt-2 text-danger";
      estado.textContent = error.message || "Error al subir la imagen.";
    } finally {
      ocupado = false;
      submits.forEach(x => x.disabled = false);
      boton.value = "";
    }
  });

  form.addEventListener("submit", e => {
    if (!ocupado) return;
    e.preventDefault();
    estado.textContent = "Termina la subida antes de guardar.";
  });
})();
