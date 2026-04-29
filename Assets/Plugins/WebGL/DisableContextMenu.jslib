mergeInto(LibraryManager.library, {
  DisableContextMenu: function () {
    if (typeof document === "undefined") {
      return;
    }

    document.addEventListener("contextmenu", function (event) {
      event.preventDefault();
    });
  }
});
