# ClassDeclarationTemplateCreator
Extension to utilize auto complete in visual studio to quickly create a class initialization template.

Below is an example on how we can quickly create an attribute (I sort of put in the snippet for the attribute for conveninet purpose), and then attach the attribute to classes I want to be able to quickly generate. It is also to prevent the extension from picking up every single classes available (trust me, we don't want that). Subsequently, we can simply type the class name and we shall see the sample in the auto-complete list.

https://github.com/YneroY/ClassDeclarationTemplateCreator/assets/5206035/2280949c-32dd-46f2-a176-eee2dbc2e1a0

There is still one thing to be done here, 
1. Include attribute value to configure how the init. of the class would be, implicit or explicit.

Overall, this should give you an idea that we could use this to create a more detailed code snippet helper with the full power of Roslyn.
