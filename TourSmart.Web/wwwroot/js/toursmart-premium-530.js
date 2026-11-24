// TourSmart RD 5.30 — mejoras progresivas y accesibles.
// Sin modificar formularios de reserva, pagos o respuestas de API.
(() => {
    "use strict";
    const header = document.getElementById("cabeceraSitio");
    if (header) {
        const update = () => header.classList.toggle("is-scrolled", window.scrollY > 12);
        window.addEventListener("scroll", update, { passive: true });
        update();
    }

    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (!reducedMotion && "IntersectionObserver" in window) {
        const targets = document.querySelectorAll(
            ".ts-premium-tour-grid > div, .ts-v3-metrics > div, .ts-v3-benefits-grid > div, .step-card"
        );
        const observer = new IntersectionObserver((entries, current) => {
            for (const entry of entries) {
                if (entry.isIntersecting) {
                    entry.target.classList.add("is-visible");
                    current.unobserve(entry.target);
                }
            }
        }, { threshold: 0.08, rootMargin: "0px 0px 40px 0px" });
        targets.forEach(node => {
            node.classList.add("ts-premium-reveal");
            observer.observe(node);
        });
    }

    // Mantener visible la navegación incluso cuando JS no puede animarla.
    // En dispositivos que reducen movimiento las tarjetas nunca se ocultan.
    const onKeyboard = event => {
        if (event.key !== "Escape") return;
        const nav = document.getElementById("mainNav");
        if (nav && nav.classList.contains("show") &&
            typeof bootstrap !== "undefined" && bootstrap.Collapse) {
            bootstrap.Collapse.getOrCreateInstance(nav, { toggle: false }).hide();
            const toggle = document.querySelector('[data-bs-target="#mainNav"]');
            if (toggle) toggle.focus();
        }
    };
    document.addEventListener("keydown", onKeyboard);

    // La barra móvil se cierra únicamente tras navegar a otro enlace.
    const collapse = document.getElementById("mainNav");
    if (collapse) {
        collapse.querySelectorAll("a[href]").forEach(link => {
            link.addEventListener("click", () => {
                if (window.innerWidth >= 992) return;
                if (typeof bootstrap === "undefined" || !bootstrap.Collapse) return;
                bootstrap.Collapse.getOrCreateInstance(collapse, { toggle: false }).hide();
            });
        });
    }
})();
