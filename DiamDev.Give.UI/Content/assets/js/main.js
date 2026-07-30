// global variables
var isIE8 = false;
var isIE9 = false;
var $windowWidth;
var $windowHeight;
var $pageArea;
//Main Function
var Main = function () {
   //function to detect explorer browser and its version
    var runInit = function () {
        if (/MSIE (\d+\.\d+);/.test(navigator.userAgent)) {
            var ieversion = new Number(RegExp.$1);
            if (ieversion == 8) {
                isIE8 = true;
            } else if (ieversion == 9) {
                isIE9 = true;
            }
        }
    };
    //function to adjust the template elements based on the window size
    var runElementsPosition = function () {
        $windowWidth = $(window).width();
        $windowHeight = $(window).height();
        $pageArea = $(window).height() - $('header').outerHeight() - $('.footer').outerHeight();
        $('.sidebar-search input').removeAttr('style').removeClass('open');
        $('.sidebar-fixed .wrap-menu').css('height', $pageArea);
        runContainerHeight();
    };
    //function to adapt the Main Content height to the Main Navigation height 
    var runContainerHeight = function () {
        mainContainer = $('.main-content > .container');
        mainNavigation = $('.main-navigation');
        if (mainContainer.outerHeight() < mainNavigation.outerHeight()) {
            mainContainer.css('min-height', mainNavigation.outerHeight());
        } else {
            mainContainer.css('min-height', '760px');
        };
    };
    //function to activate the ToDo list, if present 
    var runToDoAction = function () {
        if ($(".todo-actions").length) {
            $(".todo-actions").click(function () {
                if ($(this).find("i").attr("class") == "icon-check-empty") {
                    $(this).find("i").removeClass("icon-check-empty").addClass("icon-check");
                    $(this).parent().find("span").css({
                        opacity: .25
                    });
                    $(this).parent().find(".desc").css("text-decoration", "line-through");
                } else {
                    $(this).find("i").removeClass("icon-check").addClass("icon-check-empty");
                    $(this).parent().find("span").css({
                        opacity: 1
                    });
                    $(this).parent().find(".desc").css("text-decoration", "none");
                }
                return !1;
            });
        }
    };
    //function to activate the Tooltips, if present 
    var runTooltips = function () {
        if ($(".tooltips").length) {
            $('.tooltips').tooltip();
        }
    };
    //function to activate the Popovers, if present 
    var runPopovers = function () {
        if ($(".popovers").length) {
            $('.popovers').popover();
        }
    };
    //function to allow a button or a link to open a tab
    var runShowTab = function () {
        if ($(".show-tab").length) {
            $('.show-tab').bind('click', function (e) {
                e.preventDefault();
                var tabToShow = $(this).attr("href");
                if ($(tabToShow).length) {
                    $('a[href="' + tabToShow + '"]').tab('show');
                }
            });
        };
        if (getParameterByName('tabId').length) {
            $('a[href="#' + getParameterByName('tabId') + '"]').tab('show');
        }
    };
    //function to extend the default settings of the Accordion
    var runAccordionFeatures = function () {
        if ($('.accordion').length) {
            $('.accordion .panel-collapse').each(function () {
                if (!$(this).hasClass('in')) {
                    $(this).prev('.panel-heading').find('.accordion-toggle').addClass('collapsed');
                }
            });
        }
        $(".accordion").collapse().height('auto');
        var lastClicked;
        
        $('.accordion .accordion-toggle').bind('click', function () {
            currentTab = $(this);
            $('html,body').animate({
                scrollTop: currentTab.offset().top - 100
            }, 1000);
        });
    };
    //function to identify the current route in the Main Menu
    var runActiveNavigation = function () {
        var normalizePath = function (path) {
            var normalized = (path || '/').replace(/\/+$/, '').toLowerCase();
            return normalized || '/';
        };
        var currentPath = normalizePath(window.location.pathname);
        var activeLink = null;

        $('.main-navigation-menu a[href]').each(function () {
            var href = $(this).attr('href');
            if (!href || href === '#' || href.indexOf('javascript:') === 0) {
                return;
            }

            if (normalizePath(this.pathname) === currentPath) {
                activeLink = $(this);
                return false;
            }
        });

        if (activeLink) {
            $('.main-navigation-menu li').removeClass('active open');
            activeLink.parents('li').addClass('active open');
            activeLink.parents('ul.sub-menu').show();
            activeLink.parents('li').children('a').attr('aria-expanded', 'true');
        }
    };
    var isMobileNavigation = function () {
        return $(window).width() < 768;
    };
    var getNavigationSmallPreference = function () {
        try {
            return window.localStorage.getItem('smartK66.navigationSmall') === 'true';
        } catch (e) {
            return false;
        }
    };
    var setNavigationSmall = function (isSmall, persist) {
        var body = $('body');
        var toggler = $('.navigation-toggler');
        var topLevelLinks = $('.main-navigation-menu > li > a');

        if (isSmall && !isMobileNavigation()) {
            body.addClass('navigation-small');
        } else {
            body.removeClass('navigation-small');
            isSmall = false;
        }

        toggler.attr('aria-expanded', (!isSmall).toString());
        toggler.attr('aria-label', isSmall ? 'Expandir menu lateral' : 'Contraer menu lateral');
        topLevelLinks.each(function () {
            var link = $(this);
            var menuTitle = $.trim(link.children('.title').text());

            if (isSmall && menuTitle) {
                link.attr('title', menuTitle);
            } else {
                link.removeAttr('title');
            }
        });

        if (persist) {
            try {
                window.localStorage.setItem('smartK66.navigationSmall', isSmall.toString());
            } catch (e) {
            }
        }
    };
    //function to reduce the size of the Main Menu
    var runNavigationToggler = function () {
        var body = $('body');
        var toggler = $('.navigation-toggler');
        var sidebar = $('#app-sidebar');
        var mobileToggler = $('.navbar-toggle[aria-controls="app-sidebar"]');
        var backdrop = $('.app-sidebar-backdrop');
        var mobileMode = null;

        var setMobileNavigationOpen = function (isOpen) {
            if (isOpen) {
                body.addClass('app-navigation-open');
            } else {
                body.removeClass('app-navigation-open');
            }

            mobileToggler.attr('aria-expanded', isOpen.toString());
            mobileToggler.attr('aria-label', isOpen ? 'Cerrar menu principal' : 'Abrir menu principal');
            sidebar.attr('aria-hidden', (!isOpen).toString());
            backdrop.attr('aria-hidden', (!isOpen).toString());
            backdrop.attr('tabindex', isOpen ? '0' : '-1');
        };

        var syncNavigationMode = function () {
            var isMobile = isMobileNavigation();

            if (isMobile === mobileMode) {
                return;
            }

            mobileMode = isMobile;
            if (isMobile) {
                setNavigationSmall(false, false);
                setMobileNavigationOpen(sidebar.hasClass('in'));
            } else {
                setMobileNavigationOpen(false);
                sidebar.removeClass('in').removeAttr('style').attr('aria-hidden', 'false');
                setNavigationSmall(getNavigationSmallPreference(), false);
            }
        };

        syncNavigationMode();

        toggler.bind('click', function () {
            if (isMobileNavigation()) {
                return;
            }

            setNavigationSmall(!body.hasClass('navigation-small'), true);
        });

        sidebar
            .on('show.bs.collapse shown.bs.collapse', function () {
                if (isMobileNavigation()) {
                    setMobileNavigationOpen(true);
                }
            })
            .on('hide.bs.collapse hidden.bs.collapse', function () {
                if (isMobileNavigation()) {
                    setMobileNavigationOpen(false);
                }
            });

        backdrop.bind('click', function () {
            if (isMobileNavigation()) {
                sidebar.collapse('hide');
            }
        });

        $(document).bind('keydown.appNavigation', function (event) {
            if (event.keyCode === 27 && isMobileNavigation() && body.hasClass('app-navigation-open')) {
                sidebar.collapse('hide');
                mobileToggler.focus();
            }
        });

        $(window).bind('resize.appNavigation', syncNavigationMode);
    };
    //function to activate the panel tools
    var runModuleTools = function () {
        $('.panel-tools .panel-expand').bind('click', function (e) {
            $('.panel-tools a').not(this).hide();
            $('body').append('<div class="full-white-backdrop"></div>');
            $('.main-container').removeAttr('style');
            backdrop = $('.full-white-backdrop');
            wbox = $(this).parents('.panel');
            wbox.removeAttr('style');
            if (wbox.hasClass('panel-full-screen')) {
                backdrop.fadeIn(200, function () {
                    $('.panel-tools a').show();
                    wbox.removeClass('panel-full-screen');
                    backdrop.fadeOut(200, function () {
                        backdrop.remove();
                    });
                });
            } else {
                $('body').append('<div class="full-white-backdrop"></div>');
                backdrop.fadeIn(200, function () {
                    $('.main-container').css({
                        'max-height': $(window).outerHeight() - $('header').outerHeight() - $('.footer').outerHeight() - 100,
                        'overflow': 'hidden'
                    });
                    backdrop.fadeOut(200);
                    backdrop.remove();
                    wbox.addClass('panel-full-screen').css({
                        'max-height': $(window).height(),
                        'overflow': 'auto'
                    });;
                });
            }
        });
        $('.panel-tools .panel-close').bind('click', function (e) {
            $(this).parents(".panel").remove();
            e.preventDefault();
        });
        $('.panel-tools .panel-refresh').bind('click', function (e) {
            var el = $(this).parents(".panel");
            el.block({
                overlayCSS: {
                    backgroundColor: '#fff'
                },
                message: '<img src="assets/images/loading.gif" /> Just a moment...',
                css: {
                    border: 'none',
                    color: '#333',
                    background: 'none'
                }
            });
            window.setTimeout(function () {
                el.unblock();
            }, 1000);
            e.preventDefault();
        });
        $('.panel-tools .panel-collapse').bind('click', function (e) {
            e.preventDefault();
            var el = jQuery(this).parent().closest(".panel").children(".panel-body");
            if ($(this).hasClass("collapses")) {
                $(this).addClass("expand").removeClass("collapses");
                el.slideUp(200);
            } else {
                $(this).addClass("collapses").removeClass("expand");
                el.slideDown(200);
            }
        });
    };
    //function to activate the 3rd and 4th level menus
    var runNavigationMenu = function () {
        $('.main-navigation-menu li.active').addClass('open').children('a').attr('aria-expanded', 'true');
        $('.main-navigation-menu > li a').bind('click', function (event) {
            var link = $(this);
            var item = link.parent();
            var subMenu = item.children('ul.sub-menu');
            var isTopLevel = item.parent().hasClass('main-navigation-menu');
            var href = link.attr('href') || '';
            var expandedFromSmall = false;

            if (!subMenu.length) {
                if (isMobileNavigation() && href && href !== '#' && href.indexOf('javascript:') !== 0) {
                    $('#app-sidebar').collapse('hide');
                }
                return;
            }

            event.preventDefault();

            if (!isMobileNavigation() && $('body').hasClass('navigation-small') && isTopLevel) {
                expandedFromSmall = true;
                setNavigationSmall(false, true);
            }

            if (expandedFromSmall) {
                item.addClass('open');
                link.attr('aria-expanded', 'true');
                subMenu.stop(true, true).slideDown(200, function () {
                    runContainerHeight();
                });
                return;
            }

            if (!item.hasClass('open')) {
                item.addClass('open');
                link.attr('aria-expanded', 'true');
                item.parent().children('li.open').not(item).not($('.main-navigation-menu > li.active')).removeClass('open').children('a').attr('aria-expanded', 'false').end().children('ul').slideUp(200);
                subMenu.slideDown(200, function () {
                    runContainerHeight();
                });
            } else {
                link.attr('aria-expanded', 'false');
                if (!item.hasClass('active')) {
                    item.parent().children('li.open').not($('.main-navigation-menu > li.active')).removeClass('open').children('a').attr('aria-expanded', 'false').end().children('ul').slideUp(200, function () {
                        runContainerHeight();
                    });
                } else {
                    item.parent().children('li.open').removeClass('open').children('a').attr('aria-expanded', 'false').end().children('ul').slideUp(200, function () {
                        runContainerHeight();
                    });
                }
            }
        });
    };
    //function to activate the Go-Top button
    var runGoTop = function () {
        $('.go-top').bind('click', function (e) {
            $("html, body").animate({
                scrollTop: 0
            }, "slow");
            e.preventDefault();
        });
    };
    //function to avoid closing the dropdown on click 
    var runDropdownEnduring = function () {
        if ($('.dropdown-menu.dropdown-enduring').length) {
            $('.dropdown-menu.dropdown-enduring').click(function (event) {
                event.stopPropagation();
            });
        }
    };
    //function to return the querystring parameter with a given name.
    var getParameterByName = function (name) {
        name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
        var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
            results = regex.exec(location.search);
        return results == null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
    };
    //function to activate the iCheck Plugin 
    var runCustomCheck = function () {
        if ($('input[type="checkbox"]').length || $('input[type="radio"]').length) {
            $('input[type="checkbox"].grey, input[type="radio"].grey').iCheck({
                checkboxClass: 'icheckbox_minimal-grey',
                radioClass: 'iradio_minimal-grey',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].red, input[type="radio"].red').iCheck({
                checkboxClass: 'icheckbox_minimal-red',
                radioClass: 'iradio_minimal-red',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].green, input[type="radio"].green').iCheck({
                checkboxClass: 'icheckbox_minimal-green',
                radioClass: 'iradio_minimal-green',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].teal, input[type="radio"].teal').iCheck({
                checkboxClass: 'icheckbox_minimal-aero',
                radioClass: 'iradio_minimal-aero',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].orange, input[type="radio"].orange').iCheck({
                checkboxClass: 'icheckbox_minimal-orange',
                radioClass: 'iradio_minimal-orange',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].purple, input[type="radio"].purple').iCheck({
                checkboxClass: 'icheckbox_minimal-purple',
                radioClass: 'iradio_minimal-purple',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].yellow, input[type="radio"].yellow').iCheck({
                checkboxClass: 'icheckbox_minimal-yellow',
                radioClass: 'iradio_minimal-yellow',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].square-black, input[type="radio"].square-black').iCheck({
                checkboxClass: 'icheckbox_square',
                radioClass: 'iradio_square',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].square-grey, input[type="radio"].square-grey').iCheck({
                checkboxClass: 'icheckbox_square-grey',
                radioClass: 'iradio_square-grey',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].square-red, input[type="radio"].square-red').iCheck({
                checkboxClass: 'icheckbox_square-red',
                radioClass: 'iradio_square-red',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].square-green, input[type="radio"].square-green').iCheck({
                checkboxClass: 'icheckbox_square-green',
                radioClass: 'iradio_square-green',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].square-teal, input[type="radio"].square-teal').iCheck({
                checkboxClass: 'icheckbox_square-aero',
                radioClass: 'iradio_square-aero',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].square-orange, input[type="radio"].square-orange').iCheck({
                checkboxClass: 'icheckbox_square-orange',
                radioClass: 'iradio_square-orange',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].square-purple, input[type="radio"].square-purple').iCheck({
                checkboxClass: 'icheckbox_square-purple',
                radioClass: 'iradio_square-purple',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].square-yellow, input[type="radio"].square-yellow').iCheck({
                checkboxClass: 'icheckbox_square-yellow',
                radioClass: 'iradio_square-yellow',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].flat-black, input[type="radio"].flat-black').iCheck({
                checkboxClass: 'icheckbox_flat',
                radioClass: 'iradio_flat',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].flat-grey, input[type="radio"].flat-grey').iCheck({
                checkboxClass: 'icheckbox_flat-grey',
                radioClass: 'iradio_flat-grey',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].flat-red, input[type="radio"].flat-red').iCheck({
                checkboxClass: 'icheckbox_flat-red',
                radioClass: 'iradio_flat-red',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].flat-green, input[type="radio"].flat-green').iCheck({
                checkboxClass: 'icheckbox_flat-green',
                radioClass: 'iradio_flat-green',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].flat-teal, input[type="radio"].flat-teal').iCheck({
                checkboxClass: 'icheckbox_flat-aero',
                radioClass: 'iradio_flat-aero',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].flat-orange, input[type="radio"].flat-orange').iCheck({
                checkboxClass: 'icheckbox_flat-orange',
                radioClass: 'iradio_flat-orange',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].flat-purple, input[type="radio"].flat-purple').iCheck({
                checkboxClass: 'icheckbox_flat-purple',
                radioClass: 'iradio_flat-purple',
                increaseArea: '10%' // optional
            });
            $('input[type="checkbox"].flat-yellow, input[type="radio"].flat-yellow').iCheck({
                checkboxClass: 'icheckbox_flat-yellow',
                radioClass: 'iradio_flat-yellow',
                increaseArea: '10%' // optional
            });
        };
    };
    //Search Input function
    var runSearchInput = function () {
        var search_input = $('.sidebar-search input');
        var search_button = $('.sidebar-search button');
        var search_form = $('.sidebar-search');
        search_input.attr('data-default', $(search_input).outerWidth()).focus(function () {
            $(this).animate({
                width: 200
            }, 200);
        }).blur(function () {
            if ($(this).val() == "") {
                if ($(this).hasClass('open')) {
                    $(this).animate({
                        width: 0,
                        opacity: 0
                    }, 200, function () {
                        $(this).hide();
                    });
                } else {
                    $(this).animate({
                        width: $(this).attr('data-default')
                    }, 200);
                }
            }
        });
        search_button.bind('click', function () {
            if ($(search_input).is(':hidden')) {
                $(search_input).addClass('open').css({
                    width: 0,
                    opacity: 0
                }).show().animate({
                    width: 200,
                    opacity: 1
                }, 200).focus();
            } else if ($(search_input).hasClass('open') && $(search_input).val() == '') {
                $(search_input).removeClass('open').animate({
                    width: 0,
                    opacity: 0
                }, 200, function () {
                    $(this).hide();
                });
            } else if ($(search_input).val() != '') {
                return;
            } else $(search_input).focus();
            return false;
        });
    };
    //Set of functions for Style Selector
    var runStyleSelector = function () {
        $('#style_selector select').each(function () {
            $(this).find('option:first').attr('selected', 'selected');
        });
        $('.style-toggle').bind('click', function () {
            if ($(this).hasClass('open')) {
                $(this).removeClass('open').addClass('close');
                $('#style_selector_container').hide();
            } else {
                $(this).removeClass('close').addClass('open');
                $('#style_selector_container').show();
            }
        });
        setColorScheme();
        setLayoutStyle();
        setHeaderStyle();
        setFooterStyle();
        setBoxedBackgrounds();
    };
    $('.drop-down-wrapper').perfectScrollbar({
        wheelSpeed: 50,
        minScrollbarLength: 20,
        wheelPropagation: true
    });
    $('.navbar-tools .dropdown').on('shown.bs.dropdown', function () {
        $(this).find('.drop-down-wrapper').scrollTop(0).perfectScrollbar('update');
    });
    var setColorScheme = function () {
        $('.icons-color a').bind('click', function () {
            $('.icons-color img').each(function () {
                $(this).removeClass('active');
            });
            $(this).find('img').addClass('active');
            $('#skin_color').attr("href", "assets/css/theme_" + $(this).attr('id') + ".css");
        });
    };
    var setBoxedBackgrounds = function () {
        $('.boxed-patterns a').bind('click', function () {
            if ($('body').hasClass('layout-boxed')) {
                var classes = $('body').attr("class").split(" ").filter(function (item) {
                    return item.indexOf("bg_style_") === -1 ? item : "";
                });
                $('body').attr("class", classes.join(" "));
                $('.boxed-patterns img').each(function () {
                    $(this).removeClass('active');
                });
                $(this).find('img').addClass('active');
                $('body').addClass($(this).attr('id'));
            } else {
                alert('Select boxed layout');
            }
        });
    };
    var setLayoutStyle = function () {
        $('select[name="layout"]').change(function () {
            if ($('select[name="layout"] option:selected').val() == 'boxed')
                $('body').addClass('layout-boxed');
            else
                $('body').removeClass('layout-boxed');
        });
    };
    var setHeaderStyle = function () {
        $('select[name="header"]').change(function () {
            if ($('select[name="header"] option:selected').val() == 'default')
                $('body').addClass('header-default');
            else
                $('body').removeClass('header-default');
        });
    };
    var setFooterStyle = function () {
        $('select[name="footer"]').change(function () {
            if ($('select[name="footer"] option:selected').val() == 'fixed')
                $('body').addClass('footer-fixed');
            else
                $('body').removeClass('footer-fixed');
        });
    };
    var debounce = function (func, threshold, execAsap) {
        var timeout;
        return function debounced() {
            var obj = this,
                args = arguments;

            function delayed() {
                if (!execAsap)
                    func.apply(obj, args);
                timeout = null;
            };
            if (timeout)
                clearTimeout(timeout);
            else if (execAsap)
                func.apply(obj, args);
            timeout = setTimeout(delayed, threshold || 50);
        };
    };
    //Window Resize Function
    var runWIndowResize = function (func, threshold, execAsap) {
        //wait until the user is done resizing the window, then execute
        $(window).resize = debounce(function (e) {
            runElementsPosition();
        }, 50, false);
        $('.panel-scroll').perfectScrollbar({
            wheelSpeed: 50,
            minScrollbarLength: 20,
            wheelPropagation: true
        });
    };
    return {
        //main function to initiate template pages
        init: function () {
            runWIndowResize();
            runInit();
            runStyleSelector();
            runSearchInput();
            runElementsPosition();
            runToDoAction();
            runNavigationToggler();
            runActiveNavigation();
            runNavigationMenu();
            runGoTop();
            runModuleTools();
            runDropdownEnduring();
            runTooltips();
            runPopovers();
            runShowTab();
            runAccordionFeatures();
            runCustomCheck();
        }
    };
}();
