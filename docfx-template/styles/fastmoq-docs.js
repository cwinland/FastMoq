(function () {
  "use strict";

  function getSiteRelativePath(path) {
    var relMeta = document.querySelector("meta[property='docfx:rel']");
    var rel = relMeta ? relMeta.getAttribute("content") || "" : "";
    return rel + path;
  }

  function getBrandText() {
    var brand = document.querySelector(".navbar-brand.fastmoq-brand .fastmoq-brand-text");
    if (brand && brand.textContent) {
      return brand.textContent.trim();
    }

    var titleParts = document.title.split("|");
    if (titleParts.length > 1) {
      return titleParts[titleParts.length - 1].trim();
    }

    return "FastMoq";
  }

  function createToggleButton() {
    var button = document.createElement("button");
    button.type = "button";
    button.className = "navbar-toggle";
    button.setAttribute("data-toggle", "collapse");
    button.setAttribute("data-target", "#navbar");
    button.setAttribute("aria-controls", "navbar");
    button.setAttribute("aria-expanded", "false");
    button.setAttribute("aria-label", "Toggle navigation");

    var srOnly = document.createElement("span");
    srOnly.className = "sr-only";
    srOnly.textContent = "Toggle navigation";
    button.appendChild(srOnly);

    for (var index = 0; index < 3; index += 1) {
      var iconBar = document.createElement("span");
      iconBar.className = "icon-bar";
      button.appendChild(iconBar);
    }

    return button;
  }

  function createBrandLink() {
    var brand = document.createElement("a");
    brand.className = "navbar-brand fastmoq-brand";
    brand.href = getSiteRelativePath("index.html");

    var text = document.createElement("span");
    text.className = "fastmoq-brand-text";
    text.textContent = getBrandText();
    brand.appendChild(text);

    return brand;
  }

  function ensureNavbarHeaderScaffold() {
    var navbar = document.getElementById("autocollapse");
    if (!navbar) {
      return;
    }

    navbar.classList.add("topnav");

    var container = navbar.querySelector(".container");
    if (!container) {
      return;
    }

    var navbarBody = document.getElementById("navbar");
    var header = container.querySelector(".navbar-header");
    if (!header) {
      header = document.createElement("div");
      header.className = "navbar-header";
      container.insertBefore(header, navbarBody || container.firstChild);
    }

    var toggle = navbar.querySelector(".navbar-toggle");
    if (!toggle) {
      toggle = createToggleButton();
    }
    if (toggle.parentElement !== header) {
      header.insertBefore(toggle, header.firstChild);
    }

    var brand = navbar.querySelector(".navbar-brand.fastmoq-brand");
    if (!brand) {
      brand = createBrandLink();
    }
    if (brand.parentElement !== header) {
      header.appendChild(brand);
    }
  }

  function shouldForceCollapsed(navbar) {
    if (!navbar) {
      return false;
    }

    var topLevelMenu = document.querySelector("#navbar > ul.navbar-nav");
    if (!topLevelMenu) {
      return false;
    }

    return window.innerWidth >= 768 && topLevelMenu.querySelector("li > ul") !== null;
  }

  function syncAutoCollapse() {
    var navbar = document.getElementById("autocollapse");
    if (!navbar) {
      return;
    }

    ensureNavbarHeaderScaffold();

    navbar.classList.remove("collapsed");

    if (shouldForceCollapsed(navbar) || navbar.offsetHeight > 60) {
      navbar.classList.add("collapsed");
    }
  }

  function scheduleAutoCollapseSync() {
    window.requestAnimationFrame(function () {
      syncAutoCollapse();
      window.setTimeout(syncAutoCollapse, 100);
    });
  }

  function observeNavbarContent() {
    var navbar = document.getElementById("autocollapse");
    if (!navbar || typeof MutationObserver === "undefined") {
      return;
    }

    var observer = new MutationObserver(function () {
      scheduleAutoCollapseSync();
    });

    observer.observe(navbar, {
      childList: true,
      subtree: true,
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    observeNavbarContent();
    scheduleAutoCollapseSync();
    window.addEventListener("resize", scheduleAutoCollapseSync);
    window.addEventListener("load", scheduleAutoCollapseSync);
  });
})();