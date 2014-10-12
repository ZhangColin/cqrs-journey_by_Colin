$(function() {
    $.fn.cycle.defaults.speed = 900;
    $.fn.cycle.defaults.timeout = 6000;

    var rSec = 0;
    var rTile = 0;

    $('.tile-slide').each(function(index) {
        $(this).cycle({
            fx: 'scrollDown',
            speed: 400,
            timeout: 0
        });
    });

    AnimateTile();

    function AnimateTile() {
        rSec = Math.floor(Math.random() * 5000) + 1000;
        rTile = Math.floor(Math.random() * 5);
        setTimeout(function () {
            $('.tile-slide').eq(rTile).cycle('next');
            AnimateTile();
        }, rSec);
    }
})