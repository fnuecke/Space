(function($) {
  $(document).ready(function() {
    $("#host").click(function() {
      Screens.push("Screens/Ingame");
      Space.host();
    });
  });
})(jQuery);