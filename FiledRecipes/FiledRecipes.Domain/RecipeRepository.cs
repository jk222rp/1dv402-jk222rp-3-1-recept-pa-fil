using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FiledRecipes.Domain
{
    /// <summary>
    /// Holder for recipes.
    /// </summary>
    public class RecipeRepository : IRecipeRepository
    {
        /// <summary>
        /// Represents the recipe section.
        /// </summary>
        private const string SectionRecipe = "[Recept]";

        /// <summary>
        /// Represents the ingredients section.
        /// </summary>
        private const string SectionIngredients = "[Ingredienser]";

        /// <summary>
        /// Represents the instructions section.
        /// </summary>
        private const string SectionInstructions = "[Instruktioner]";

        /// <summary>
        /// Occurs after changes to the underlying collection of recipes.
        /// </summary>
        public event EventHandler RecipesChangedEvent;

        /// <summary>
        /// Specifies how the next line read from the file will be interpreted.
        /// </summary>
        private enum RecipeReadStatus { Indefinite, New, Ingredient, Instruction };

        /// <summary>
        /// Collection of recipes.
        /// </summary>
        private List<IRecipe> _recipes;

        /// <summary>
        /// The fully qualified path and name of the file with recipes.
        /// </summary>
        private string _path;

        /// <summary>
        /// Indicates whether the collection of recipes has been modified since it was last saved.
        /// </summary>
        public bool IsModified { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the RecipeRepository class.
        /// </summary>
        /// <param name="path">The path and name of the file with recipes.</param>
        public RecipeRepository(string path)
        {
            // Throws an exception if the path is invalid.
            _path = Path.GetFullPath(path);

            _recipes = new List<IRecipe>();
        }

        /// <summary>
        /// Returns a collection of recipes.
        /// </summary>
        /// <returns>A IEnumerable&lt;Recipe&gt; containing all the recipes.</returns>
        public virtual IEnumerable<IRecipe> GetAll()
        {
            // Deep copy the objects to avoid privacy leaks.
            return _recipes.Select(r => (IRecipe)r.Clone());
        }

        /// <summary>
        /// Returns a recipe.
        /// </summary>
        /// <param name="index">The zero-based index of the recipe to get.</param>
        /// <returns>The recipe at the specified index.</returns>
        public virtual IRecipe GetAt(int index)
        {
            // Deep copy the object to avoid privacy leak.
            return (IRecipe)_recipes[index].Clone();
        }

        /// <summary>
        /// Deletes a recipe.
        /// </summary>
        /// <param name="recipe">The recipe to delete. The value can be null.</param>
        public virtual void Delete(IRecipe recipe)
        {
            // If it's a copy of a recipe...
            if (!_recipes.Contains(recipe))
            {
                // ...try to find the original!
                recipe = _recipes.Find(r => r.Equals(recipe));
            }
            _recipes.Remove(recipe);
            IsModified = true;
            OnRecipesChanged(EventArgs.Empty);
        }

        /// <summary>
        /// Deletes a recipe.
        /// </summary>
        /// <param name="index">The zero-based index of the recipe to delete.</param>
        public virtual void Delete(int index)
        {
            Delete(_recipes[index]);
        }

        /// <summary>
        /// Raises the RecipesChanged event.
        /// </summary>
        /// <param name="e">The EventArgs that contains the event data.</param>
        protected virtual void OnRecipesChanged(EventArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of 
            // a race condition if the last subscriber unsubscribes 
            // immediately after the null check and before the event is raised.
            EventHandler handler = RecipesChangedEvent;

            // Event will be null if there are no subscribers. 
            if (handler != null)
            {
                // Use the () operator to raise the event.
                handler(this, e);
            }
        }





//Komplettering, med metoderna som saknas!

        //Läser in recept
        public void Load()
        {
            try
            {
                //Skapar en lista som kan innehålla referenser till receptobjekt
                List<IRecipe> recipes = new List<IRecipe>();

                //Öppnar textfilen
                using (StreamReader reader = new StreamReader(_path))
                {
                    //Variabler
                    string line;
                    RecipeReadStatus status = RecipeReadStatus.Indefinite;
                    Recipe theRecipe = null;

                    //Läser raderna från textfilen tills det är slut på filen
                    while ((line = reader.ReadLine()) != null)
                    {
                        //Om det är en tom rad läses nästa rad in utan att gå igenom andra if:s
                        if (line != "")
                        {
                            if (line == SectionRecipe) //Om det är en avdelning för nytt recept
                            {
                                //Sätter status till att nästa rad som läses in kommer att vara receptets namn
                                status = RecipeReadStatus.New;
                            } else if (line == SectionIngredients) { //Om det är avdelningen för ingredienser
                                //Sätter status till att kommande rader som läses in kommer att vara receptets ingredienser
                                status = RecipeReadStatus.Ingredient;
                            } else if (line == SectionInstructions) { //Om det är avdelningen för instruktioner
                                //Sätter status till att kommande rader som läses in kommer att vara receptets instruktioner
                                status = RecipeReadStatus.Instruction;
                            } else { //Annars är det ett namn, en ingrediens eller en instruktion
                                //Kollar vad raden ska tolkas som
                                switch (status)
                                {
                                    case RecipeReadStatus.New: //Om status är satt att raden ska tolkas som ett recepts namn

                                        //Lägger föregående recept i listan med recept, om inte receptobjektet är tomt
                                        if (theRecipe != null)
                                        {
                                            //Lägger till föregående recept till listan med recept innan receptobjektet skrivs över
                                            recipes.Add(theRecipe);
                                        }

                                        //"Skapar nytt" receptobjekt med receptets namn
                                        theRecipe = new Recipe(line);

                                        break;

                                    case RecipeReadStatus.Ingredient: //Om status är att raden ska tolkas som en ingrediens
                                        
                                        //Delar upp raden i delar
                                        string[] values = line.Split(new char[] { ';' });

                                        //Kastar undantag om antalet delar inte är tre i raden med ingrediensen
                                        if (values.Length != 3)
                                        {
                                            throw new FileFormatException("Antalet delar i raden med ingrediensen är inte tre");
                                        }

                                        //Skapar ett ingrediensobjekt och initierar det med de tre delarna för mängd, mått och namn
                                        Ingredient ingredient = new Ingredient();
                                        ingredient.Amount = values[0];
                                        ingredient.Measure = values[1];
                                        ingredient.Name = values[2];

                                        //Lägger till ingrediensen till receptets lista med ingredienser
                                        theRecipe.Add(ingredient);

                                        break;

                                    case RecipeReadStatus.Instruction: //Om status är satt att raden ska tolkas som en instruktion

                                        //Lägger till raden till receptets lista med instruktioner
                                        theRecipe.Add(line);

                                        break;

                                    default: //Kastar undantag om inget av ovanstående fall stämmer
                                        throw new FileFormatException("Något är fel!");

                                }
                            }
                        }
                    }
                    //Lägger till det sista receptet i listan med recept
                    recipes.Add(theRecipe);
                }

                //Tar bort tomma platser i listan med recept
                recipes.TrimExcess();

                //Sorterar listan med recept med avseende på receptens namn
                IEnumerable<IRecipe> sortedRecipes = recipes.OrderBy(recipe => recipe.Name);
                
                //Tilldelar _recipes en referens till den sorterade listan
                _recipes = new List<IRecipe>(sortedRecipes);

                //Indikerar att listan med recept är oförändrad
                IsModified = false;

                //Utlöser händelse om att recept har lästs in
                OnRecipesChanged(EventArgs.Empty);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        //Öppnar en textfil och skriver recepten rad för rad till textfilen
        public void Save()
        {
            try
            {
                //Öppnar textfilen som recepten ska skrivas till
                using (StreamWriter writer = new StreamWriter(_path))
                {
                    //För varje recept i listan med recept
                    foreach (Recipe recipe in _recipes)
                    {
                        //Skriver rad med formatering för avdelningen med recept och sedan receptets namn
                        writer.WriteLine(SectionRecipe);
                        writer.WriteLine(recipe.Name);

                        //Skriver rad med formatering för avdelningen med ingredienser
                        writer.WriteLine(SectionIngredients);
                        //Skriver ut varje ingrediens i receptet med formatering för ingredienserna
                        foreach (Ingredient ingredient in recipe.Ingredients)
                        {
                            writer.WriteLine("{0};{1};{2}", ingredient.Amount, ingredient.Measure, ingredient.Name);
                        }

                        //Skriver rad med formatering för avdelningen med instruktioner
                        writer.WriteLine(SectionInstructions);
                        //Skriver ut varje instruktion i receptet
                        foreach (string instruction in recipe.Instructions)
                        {
                            writer.WriteLine(instruction);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
