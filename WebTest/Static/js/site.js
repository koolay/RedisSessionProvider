$(document)
  .ready(function () {
      var
        changeSides = function () {
            $('.ui.shape')
              .eq(0)
                .shape('flip over')
                .end()
              .eq(1)
                .shape('flip over')
                .end()
              .eq(2)
                .shape('flip back')
                .end()
              .eq(3)
                .shape('flip back')
                .end()
            ;
        },
        validationRules = {
            username: {
                identifier: 'UserName',
                rules: [
                  {
                      type: 'empty',
                      prompt: '用户名不能为空'
                  },
                  {
                      type: 'length[16]',
                      prompt: '用户名长度不能超过16'
                  }
                ]
            },
            password: {
                identifier: 'Password',
                rules: [{ type: 'empty', prompt: '密码不能为空'}]
            }
        }
      ;

      $('.ui.dropdown').dropdown({on: 'hover'});

     // $('.ui.form').form(validationRules, {on: 'blur'});
      $('.ui.checkbox').checkbox();
      $('.masthead .information').transition('scale in');
      $('.message .close').on('click', function () {
          $(this).closest('.message').fadeOut();
      });
      setInterval(changeSides, 3000);


  })
;
